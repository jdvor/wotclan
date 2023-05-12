# WotClan.Personnel (wcp) Console Application

Console (terminal) application which is able to scrape Military Personnel summary of the Wargaming's Clan Portal.

* [Changelog](changelog.md)
* [Roadmap](roadmap.md)

## Installation

It's single executable file with no depenencies, so it can be just copied to any location and it would work.

## Usage

Print clan players summary to terminal using defaults (any battle mode, all available history).
```shell
wcp fetch {clan-id}
```

Download clan players summary for skirmish mode within last 28 days and store it as CSV file in specified directory.
```shell
wcp fetch {clan-id} -t Days28 -b Skirmish -o csv -d {directory-for-result}
```

## Command: fetch
Download or print players summary for given battle mode and available time period.

Unless specific name for the output file is provided one is derived automatically based on current date, 
selected timeframe and battle type.<br/>
The pattern is `yyyy-MM-dd_HHmmss_{battle-type}_{time-frame}.{extension}`, so for example the result could be 
`2023-05-12_195204_skirmish_28d.xlsx`.

```shell
wcp fetch {clan-id} [ options ]
```

**positional arguments:**

| position | code    | value type | description       |
|----------|---------|------------|-------------------|
| 0        | clan-id | int32      | Wargaming clan ID |

**options:**

| option                 | value type | default      | description                                                                                                                                         |
|------------------------|------------|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| -t, --timeframe        | enum       | AllAvailable | valid values: AllAvailable, Days28, Days7, Day                                                                                                      |
| -b, --battle-type      | enum       | Default      | valid values: Default, Team, Skirmish, Advance, GlobalMap; Default = Random + Tank Company + GlobalMap                                              |
| -o, --output-type      | enum       | None         | valid values: None, Table, Csv, Xlsx                                                                                                                |
| -d, --output-directory | string     | "./"         | path to directory to which output files are saved; filenames are generated automatically based on current date, selected timeframe and battle type. |
| -f, --output-file      | string     |              | expected full path; if provided, output directory is ignored and output type determined based on file extension                                     |
| -r, --region           | enum       | EU           | valid values: EU, RU, NA, ASIA                                                                                                                      |
