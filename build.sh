#!/usr/bin/env bash

set -o pipefail
set -e
pushd . > /dev/null

### Pre-requisite CLI tools ###
# dotnet - needs to be installed, https://dotnet.microsoft.com/en-us/download/dotnet/7.0
# git - needs to be installed, `sudo apt install git`
# zip - needs to be installed, `sudo apt install zip`
# realpath - most Linux distros have it, some don't
# tar, mkdir, grep, rm, find, sed, awk - all Linux distros have it

### Expected solution organization ###
#
# ./XXX.sln
# ./build.sh
# ./Dockerfile.build
# ./src/XXX/XXX.csproj
# ./tests/XXX.(Tests|IntegrationTests)/XXX.(Tests|IntegrationTests).csproj
#
# * All projects should be by default IsPackable=false and IsPublishable=false
# * NuGet package projects must have IsPublishable=true within csproj file
# * Application projects must have IsPackable=true within csproj file
# * If application projects should be published for more RIDs, than they must have RuntimeIdentifiers property within csproj file

build_apps=''
build_packages=''
ci_build='False'
ci_build_no=0
default_rid='linux-x64'
deps_dir=$(realpath './.deps/')
docker=''
quality='relaxed'
output_dir=$(realpath './publish')
skip_restore='False'
semver=''
suffix=''
tests=''

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -v|--semver) semver="$2"; shift ;;
        -s|--suffix) suffix="$2"; shift ;;
        -c|--ci-build-no) ci_build_no=$(($2)); shift ;;
        -o|--output-dir) output_dir=$(realpath $2); shift ;;
        -t|--tests) tests="$2"; shift ;;
        -d|--docker) docker="$2"; shift ;;
        -q|--quality) quality="$2"; shift ;;
        -x|--default-rid) default_rid="$2"; shift ;;
        -a|--skip-apps) build_apps='False' ;;
        -p|--skip-packages) build_packages='False' ;;
        -r|--skip-restore) skip_restore='True' ;;
        *) echo "Unknown parameter passed: $1"; exit 1 ;;
    esac
    shift
done

if (( $ci_build_no > 0 )); then
    ci_build=True
fi

# If not passed as argument, try to find semantic version in git tags.
if [ -z "$semver" ]; then
    ! semver=$(git tag 2> /dev/null | grep -P '^\d+\.\d+\.\d+(-[a-z0-9]+)?$' | sort -V | tail -1)
    if [ -z "$semver" ]; then
        if (( $ci_build_no > 0 )); then
            echo 'Failed to determine package version' 1>&2
            exit 1
        else
            semver='1.0.0'
        fi
    fi
fi

# If not passed as argument, try to create version suffix from name of the git branch.
if [ -z "$suffix" ]; then
    ! git_branch=$(git rev-parse --abbrev-ref HEAD 2> /dev/null)
    if [ "$git_branch" != 'main' ] && [ "$git_branch" != 'master' ]; then
        suffix=$(echo "$git_branch" | tr '[:upper:]' '[:lower:]' | sed s/[^a-z0-9]//g)
    fi
fi

if [ -z "$build_packages" ]; then
    packages=($(grep -riwl ./src -e '<IsPackable>true</IsPackable>' --include=\*.{csproj,props} --exclude-dir={bin,obj} || :))
    #printf '%s\n' "${packages[@]}"
    [[ -n "$packages" ]] && build_packages='True' || build_packages='False'
fi

if [ -z "$build_apps" ]; then
    apps=($(grep -riwl ./src -e '<IsPublishable>true</IsPublishable>' --include=\*.csproj --exclude-dir={bin,obj} || :))
    #printf '%s\n' "${apps[@]}"
    [[ -n "$apps" ]] && build_apps='True' || build_apps='False'
fi

run_tests='True'
test_filter='FullyQualifiedName!~.IntegrationTests.'
test_logger='console;verbosity=normal;consoleLoggerParameters=ErrorsOnly'
if [ -z "$tests" ] || [ "$tests" == 'dev' ]; then
    : # no-op
elif [ "$tests" == 'teamcity' ]; then
    test_logger=''
elif [ "$tests" == 'skip' ]; then
    run_tests='False'
else
    test_filter="$tests"
fi

(( $ci_build_no > 0 )) && version="$semver.$ci_build_no" || version=$semver
[[ -z "$suffix" ]] && full_version=$version || full_version="$version-$suffix"
[[ "$quality" == 'strict' ]] && warn_as_err='True' || warn_as_err='False'

# ########################################
# Build everything in docker (if requested)
# ########################################
# Executes this script within a docker container described in Dockerfile.build.
if [ -n "$docker" ]; then

    # Copies restore dependencies such as csproj files to .deps/
    # It allows for caching when building the container.
    rm -rf $deps_dir
    mkdir -p $deps_dir
    find . -iname 'bin' -or -iname 'obj' | xargs rm -rf
    find . -name '*.csproj' -or -name '*.props' -or -name '*.sln' -or -name 'nuget.config' | xargs -I {} cp {} $deps_dir --parents

    # derives name of the container from the root directory name
    if [ "$docker" == 'auto' ]; then
        docker=${PWD##*/}
    fi

    image_tag="$docker/build:$full_version"

    image_id=$(docker images $image_tag -q)
    if [ -n "$image_id" ]; then
        echo "removing $image_tag ($image_id)"
        docker rmi $image_id --force 1> /dev/null
    fi

    docker build -f Dockerfile.build -t $image_tag --progress=auto \
        --build-arg VERSION_PREFIX=$version \
        --build-arg VERSION_SUFFIX=$suffix \
        --build-arg CI_BUILD_NO=$ci_build_no \
        --build-arg QUALITY=$quality \
        .

    popd  > /dev/null
    exit 0
fi


# print debugging information
echo "version: $full_version"
echo "CI: $ci_build ($ci_build_no)"
echo "quality: $quality"
echo "tests: $run_tests (filter='$test_filter')"
echo "build packages: $build_packages"
echo "build apps: $build_apps"
echo "cwd: $(pwd)"
echo "output: $output_dir"

# clean up
rm -rf $output_dir

# #########
# Run tests
# #########
if [ "$run_tests" == 'True' ]; then
    echo
    echo 'Tests >>>'

    # https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit

    #  --logger "trx;LogFileName=<TestResults.trx>"
    #  --filter "FullyQualifiedName~Namespace.Class"
    # --blame-hang-timeout 60s
    # -e VARIABLE=abc
    # -l "console;verbosity=normal"
    results_dir="$output_dir/test-results"
    mkdir -p "$results_dir"

    args=('test' '-c' 'Release' '--nologo' '-v' 'quiet' \
        '--filter' "$test_filter" \
        '--results-directory' "$results_dir" \
        '--logger' "$test_logger" \
        # '-p:CollectCoverage' \
        # '-p:CoverletOutputFormat=opencover' \
        # "-p:CoverletOutput=\"$results_dir/coverage.xml\"" \
        "-p:TreatWarningsAsErrors=$warn_as_err")
    [[ "$quality" != 'strict' ]] && args+=('-clp:ErrorsOnly')
    [[ "$skip_restore" == 'True' ]] && args+=('--no-restore')

    dotnet "${args[@]}"
fi


# ####################
# Build NuGet packages
# ####################
if [ "$build_packages" == 'True' ]; then
    echo
    echo 'NuGet Packages >>>'

    args=('pack' '-c' 'Release' '--nologo' \
        '-o' "$output_dir" \
        "-p:VersionPrefix=\"$version\"" \
        "-p:VersionSuffix=\"$suffix\"" \
        "-p:TreatWarningsAsErrors=$warn_as_err" \
        "-p:ContinuousIntegrationBuild=$ci_build" )
    [[ "$quality" != 'strict' ]] && args+=('-clp:ErrorsOnly')
    [[ "$skip_restore" == 'True' ]] && args+=('--no-restore')

    dotnet "${args[@]}"
fi

# ##################
# Build applications
# ##################
if [ "$build_apps" == 'True' ]; then
    for path in "${apps[@]}"; do
        app=${path##*/}
        app="${app%.csproj}"
        rids=$(grep -i '<RuntimeIdentifiers>' $path | tr -dc '[[:print:]]')
        if [ -z "$rids" ]; then
            rids=($default_rid)
        else
            rids=$(echo $rids | sed -nE 's/<RuntimeIdentifiers>\s*([ a-z0-9;-]+)\s*<\/RuntimeIdentifiers>/\1/p')
            rids=(${rids//;/ })
            #printf '%s\n' "${rids[@]}"
        fi

        for rid in "${rids[@]}"; do
            rid=$(echo $rid | tr -dc '[[:print:]]')
            app_output_dir="$output_dir/${app}_${full_version}/$rid"
            echo
            echo "$app ($rid) >>>"
            args=('publish' $path '-c' 'Release' '--nologo' \
                '-o' "$app_output_dir" \
                '-r' $rid \
                "-p:VersionPrefix=\"$version\"" \
                "-p:VersionSuffix=\"$suffix\"" \
                "-p:TreatWarningsAsErrors=$warn_as_err" \
                "-p:ContinuousIntegrationBuild=$ci_build" )
            [[ "$quality" != 'strict' ]] && args+=('-clp:ErrorsOnly')

            # dotnet restore does not seem to work for multi-RID projects
            # even when the project contains correct RuntimeIndetifiers property.
            # So for applications it is safer not to skip restore.
            #[[ "$skip_restore" == 'True' ]] && args+=('--no-restore')

            dotnet "${args[@]}"

            find $app_output_dir -name '*.pdb' | xargs -r rm

            if [[ "$rid" == 'win'* ]]; then
                zip -jq "$output_dir/${app}_${full_version}_$rid.zip" $app_output_dir/*
            else
                tar -C "$app_output_dir" -czf "$output_dir/${app}_${full_version}_$rid.tar.gz" .
            fi
        done

        rm -rf "$output_dir/${app}_${full_version}"
    done
fi

popd > /dev/null
