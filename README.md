# UCSD_DataMining_ChestWall

One-off data-mining console app that finds chest wall (post-mastectomy) radiotherapy patients with reconstruction by scanning local JSON exports of the treatment-planning database, then writes the matching cohort to a CSV. Not a maintained tool — paths and selection criteria are hard-coded for a specific analysis.

## How it works

Everything lives in `FindingCWPatients.cs` (`Main` plus one filter function):

1. Loads patient JSON files (produced by the companion `Make_Raystation_Data_StructureCSharp` projects) from a local `LocalDatabases\<year>` folder.
2. Keeps external-beam plans prescribed 200 cGy x 25 fractions, skipping plan names that indicate other treatment sites (lung, eso, panc, abd, pelv, SCL/SCV, mediastinum, etc.).
3. Requires a `heart` dose ROI and excludes plans with a tumor-"bed" ROI, selecting chest wall rather than intact-breast cases.
4. Flags reconstruction by finding unclassified ROIs with a `CustomHU` material (density) override receiving >= 5 Gy, recording that ROI's name, volume, and dose.
5. Writes MRN, treatment date, course, diagnosis codes, plan name, dose/fractionation, and the artifact ROI fields to `ChestWallPatients.csv` on a network share.

## Requirements

- .NET 8 console app; Newtonsoft.Json.
- Project references into a sibling checkout of `Make_Raystation_Data_StructureCSharp` (`DataBaseStructure`, `DataWritingTools`, `DataBaseFileManager`) for the patient database classes and JSON reader.

## Usage

Edit the hard-coded input directory (`LocalDatabases\<year>`) and output path in `Main`, then build and run the executable. Output is a single CSV of candidate patients for manual review.
