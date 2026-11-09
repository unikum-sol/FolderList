# FolderList

A small Windows (WinForms) program with a graphical interface that exports the **complete folder and file structure of a directory** as:

- **TXT:** a readable tree view
- **JSON:** a machine-readable structure, for example for further processing in scripts or for handing to an AI

The program reads only **names** and the structure. File contents, sizes and dates are not captured. It does not modify the source folder.

Example of the TXT output (the program's own labels are in German):

```
Ordnerstruktur von: D:\Projekte
Erstellt am:        09.11.2026 07:30:00
Legende:            Ordner enden mit \
======================================================================

D:\Projekte\
├── Alpha\
│   ├── src\
│   │   └── main.cs
│   └── readme.txt
└── notizen.txt

======================================================================
Ordner: 3   Dateien: 3   Verknüpfungen (nicht verfolgt): 0   Fehler: 0
```

("Ordnerstruktur von" = folder structure of, "Erstellt am" = created on, "Legende: Ordner enden mit \" = legend: folders end with \, "Ordner / Dateien / Verknüpfungen (nicht verfolgt) / Fehler" = folders / files / links (not followed) / errors.)

---

## Contents

- [Features](#features)
- [Requirements](#requirements)
- [Files in the repository](#files-in-the-repository)
- [Building](#building)
- [Usage](#usage)
- [Output files](#output-files)
- [Behavior in detail](#behavior-in-detail)
- [Notes and limitations](#notes-and-limitations)
- [Troubleshooting](#troubleshooting)
- [License](#license)

---

## Features

- Exports the structure of any folder **recursively** into a TXT and a JSON file.
- **Folders first, then files**, each sorted alphabetically (case-insensitive).
- **Progress display** and progress log in the window.
- **Cancel** at any time. Incomplete files are deleted.
- Folders without access are **marked as errors** and the export continues.
- **Junctions and symbolic folder links** are marked but not followed (protection against endless loops).
- Designed for **very deep folder structures** and **long paths**.
- Runs the work in a separate thread, so the interface stays responsive.
- The **Zielordner öffnen** (open target folder) button starts Windows Explorer.

---

## Requirements

| Component | Note |
|---|---|
| 64-bit Windows | The program is compiled as x64 (`/platform:x64`) |
| .NET Framework 4.x | Provides the compiler `csc.exe` (C# 5). The configuration file names .NET Framework 4.8 |
| Administrator rights | **Not required.** The manifest starts the program as the calling user (`asInvoker`) |

No development environment needs to be installed.

---

## Files in the repository

| File | Purpose |
|---|---|
| `FolderList.cs` | Source code |
| `app.manifest` | Manifest: start without administrator rights (`asInvoker`), long path support (`longPathAware`) |
| `FolderList.exe.config` | Runtime configuration: .NET Framework 4.8, settings for long paths |
| `build.cmd` | Build script that compiles the EXE |

> If you renamed files with `.txt` to upload them: in the repository they must be named `FolderList.cs`, `app.manifest` and `build.cmd` again. The build script expects exactly these names.

> **Important:** `FolderList.exe.config` must **stay next to the EXE** and have exactly this name. The build script reminds you of this at the end.

---

## Building

1. Put all files into the same folder.
2. Double-click `build.cmd`.

The script looks for `csc.exe` in `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\` and runs this command:

```cmd
csc.exe /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:FolderList.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll FolderList.cs
```

`/codepage:65001` means the source is read as UTF-8 (because of the umlauts in the texts). On success it prints `BUILD OK: <path>\FolderList.exe`, otherwise `BUILD FEHLGESCHLAGEN` (build failed). If the compiler cannot be found, the script reports this and exits.

> The messages of `build.cmd` are in German.

---

## Usage

1. Start `FolderList.exe`.
2. **Quellordner** (source folder): choose the folder whose structure should be exported (field or **Durchsuchen…** / Browse).
3. **Zielordner** (target folder): choose the folder where TXT and JSON are saved. The default is the **Desktop**.
4. Click **Exportieren** (Export).
5. During the export, the status line shows progress (every 500 entries) and the log shows the paths used. **Abbrechen** (Cancel) stops the export.
6. At the end, the log shows `FERTIG nach hh:mm:ss` (finished after …) with the number of folders, files, links and errors.
7. **Zielordner öffnen** shows the generated files in Explorer.

Both folders must **exist**, otherwise an error message appears. The user interface is in German.

---

## Output files

Both files are placed in the target folder and share the same name with different extensions:

```
Struktur_<foldername>_<YYYY-MM-DD>_<HHMMSS>.txt
Struktur_<foldername>_<YYYY-MM-DD>_<HHMMSS>.json
```

- Invalid characters in the folder name are replaced by `_`.
- For a **drive root** (for example `D:\`) the name part is `Laufwerk_D`.
- If nothing is left, `Ordner` is used.

### TXT

- Encoding UTF-8 with BOM, CRLF line endings.
- Header lines with source folder, creation time and legend (folders end with `\`).
- Tree view with `├──`, `└──` and `│`.
- Links appear as `Name\  [Verknüpfung - nicht verfolgt]` (link, not followed).
- Folders without access appear with `[FEHLER: …]` (error).
- A summary with the counts is at the end.

### JSON

- Encoding UTF-8 without BOM, LF line endings.
- Structure:

```json
{
  "format": "FolderList",
  "version": 1,
  "root": "D:\\Projekte",
  "created": "2026-11-09T07:30:00+01:00",
  "pathSeparator": "\\",
  "tree": {
    "type": "folder",
    "name": "Projekte",
    "path": "",
    "children": [
      { "type": "file", "name": "notizen.txt", "path": "notizen.txt" }
    ]
  },
  "summary": { "folders": 1, "files": 1, "linksNotFollowed": 0, "errors": 0 }
}
```

| Field | Meaning |
|---|---|
| `type` | `folder`, `file` or `link` |
| `name` | Name of the entry |
| `path` | Path **relative to the source folder**, with `\` as separator. Empty for the root folder |
| `children` | Contents of a folder |
| `error` | Only for folders that could not be read: error message |
| `summary` | Number of folders, files, links not followed and errors |

---

## Behavior in detail

- **Sorting:** All folders first, then all files, each by name, case-insensitive.
- **Links:** Folders that are junctions or symbolic links (the "reparse point" attribute) are listed but **not entered**. They are counted under "links".
- **Access errors:** If a folder cannot be read (for example "access denied"), it is marked with the error message and counted. The export continues. When errors occur, the program suggests starting it as administrator.
- **Cancel or error:** If the export is cancelled or a serious error occurs, the incomplete TXT and JSON files are **deleted**.
- **Closing:** While an export is running, the window cannot be closed. Cancel first.
- **Counting "folders":** The source folder itself is counted.
- **Memory:** The program writes directly to the files while traversing and does not keep the whole tree in memory. The entries of a single directory are briefly held in lists for sorting.

---

## Notes and limitations

- **Names only:** Sizes, dates, attributes and file contents are not output.
- **No filter:** The code does not hide any files or folders, including hidden or system files, as far as the user is allowed to read them.
- **Target folder inside the source folder:** The code does not exclude the target folder. If it lies inside the source folder, the output files might appear in the listing. I have not tested this. It is best to choose a target folder outside the source folder.
- **File links:** Only **folders** with the "reparse point" attribute are counted as links. Links to files are listed like files.
- **Long paths:** The manifest and the configuration file enable long-path support. Whether it takes effect on your system depends on the Windows configuration and has not been verified here.
- **Read-only access:** The program writes only the two output files in the target folder.

---

## Troubleshooting

| Problem | Possible cause / solution |
|---|---|
| `BUILD FEHLGESCHLAGEN` (build failed) | Read the messages with `error CS…` above that line. Check that `FolderList.cs` and `app.manifest` are in the same folder |
| "csc.exe nicht gefunden" (csc.exe not found) | .NET Framework 4.x (64-bit) is missing or installed elsewhere |
| Umlauts shown incorrectly in the program | `FolderList.cs` must be saved as UTF-8. The build script reads it with `/codepage:65001` |
| Program does not start or reports a configuration error | Is `FolderList.exe.config` next to the EXE and named exactly that? |
| "Quellordner existiert nicht" / "Zielordner existiert nicht" (source/target folder does not exist) | Check the paths. Both folders must exist |
| The result contains `[FEHLER: …]` entries | The user is not allowed to read these folders. Start as administrator if needed |
| Window cannot be closed | An export is still running. Click **Abbrechen** (Cancel) first |

---

