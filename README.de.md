# FolderList

Ein kleines Windows-Programm (WinForms) mit grafischer Oberfläche, das die **komplette Ordner- und Dateistruktur eines Ordners** exportiert, und zwar als:

- **TXT:** lesbare Baumdarstellung
- **JSON:** maschinenlesbare Struktur, z. B. zur Weiterverarbeitung in Skripten oder zur Übergabe an eine KI

Das Programm liest nur **Namen** und die Struktur. Dateiinhalte, Größen und Datumsangaben werden nicht erfasst. Es verändert nichts am Quellordner.

Beispiel der TXT-Ausgabe:

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

---

## Inhalt

- [Funktionen](#funktionen)
- [Voraussetzungen](#voraussetzungen)
- [Dateien im Repository](#dateien-im-repository)
- [Kompilieren](#kompilieren)
- [Bedienung](#bedienung)
- [Ausgabedateien](#ausgabedateien)
- [Verhalten im Detail](#verhalten-im-detail)
- [Hinweise und Einschränkungen](#hinweise-und-einschränkungen)
- [Fehlerbehebung](#fehlerbehebung)
- [Lizenz](#lizenz)

---

## Funktionen

- Exportiert die Struktur eines beliebigen Ordners **rekursiv** in eine TXT- und eine JSON-Datei.
- **Ordner zuerst, dann Dateien**, jeweils alphabetisch sortiert (ohne Beachtung der Groß-/Kleinschreibung).
- **Fortschrittsanzeige** und Fortschrittsprotokoll im Fenster.
- **Abbrechen** jederzeit möglich. Unvollständige Dateien werden dabei gelöscht.
- Ordner ohne Zugriff werden **als Fehler markiert**, der Export läuft weiter.
- **Junctions und symbolische Ordner-Links** werden gekennzeichnet, aber nicht verfolgt (Schutz vor Endlosschleifen).
- Ausgelegt für **sehr tiefe Ordnerstrukturen** und **lange Pfade**.
- Eigene Arbeitsschleife in einem separaten Thread, die Oberfläche bleibt bedienbar.
- Schaltfläche **Zielordner öffnen** startet den Windows-Explorer.

---

## Voraussetzungen

| Komponente | Hinweis |
|---|---|
| 64-Bit-Windows | Das Programm wird als x64 kompiliert (`/platform:x64`) |
| .NET Framework 4.x | Liefert den Compiler `csc.exe` (C# 5). Die Konfigurationsdatei nennt .NET Framework 4.8 |
| Administratorrechte | **Nicht nötig.** Das Manifest startet das Programm als aufrufender Benutzer (`asInvoker`) |

Es muss keine Entwicklungsumgebung installiert werden.

---

## Dateien im Repository

| Datei | Zweck |
|---|---|
| `FolderList.cs` | Quellcode |
| `app.manifest` | Manifest: Start ohne Administratorrechte (`asInvoker`), Unterstützung langer Pfade (`longPathAware`) |
| `FolderList.exe.config` | Laufzeitkonfiguration: .NET Framework 4.8, Einstellungen für lange Pfade |
| `build.cmd` | Build-Skript, das die EXE kompiliert |

> Falls du Dateien zum Hochladen mit `.txt` umbenannt hast: Im Repository müssen sie wieder `FolderList.cs`, `app.manifest` und `build.cmd` heißen. Das Build-Skript erwartet genau diese Namen.

> **Wichtig:** `FolderList.exe.config` muss **neben der EXE liegen bleiben** und genau so heißen. Das Build-Skript weist am Ende darauf hin.

---

## Kompilieren

1. Alle Dateien in denselben Ordner legen.
2. `build.cmd` per Doppelklick starten.

Das Skript sucht `csc.exe` unter `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\` und führt diesen Befehl aus:

```cmd
csc.exe /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:FolderList.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll FolderList.cs
```

`/codepage:65001` bedeutet, dass der Quelltext als UTF-8 gelesen wird (wegen der Umlaute in den Texten). Bei Erfolg erscheint `BUILD OK: <Pfad>\FolderList.exe`, sonst `BUILD FEHLGESCHLAGEN`. Findet das Skript den Compiler nicht, wird das gemeldet und das Skript beendet.

---

## Bedienung

1. `FolderList.exe` starten.
2. **Quellordner:** Ordner wählen, dessen Struktur exportiert werden soll (Feld oder **Durchsuchen…**).
3. **Zielordner:** Ordner wählen, in dem TXT und JSON gespeichert werden. Voreingestellt ist der **Desktop**.
4. **Exportieren** klicken.
5. Während des Exports zeigt die Statuszeile den Fortschritt (alle 500 Einträge) und das Protokoll die verwendeten Pfade. Mit **Abbrechen** lässt sich der Export stoppen.
6. Am Ende zeigt das Protokoll `FERTIG nach hh:mm:ss` mit der Anzahl von Ordnern, Dateien, Verknüpfungen und Fehlern.
7. **Zielordner öffnen** zeigt die erzeugten Dateien im Explorer.

Beide Ordner müssen **existieren**, sonst erscheint eine Fehlermeldung. Die Oberfläche ist auf Deutsch.

---

## Ausgabedateien

Beide Dateien liegen im Zielordner und haben denselben Namen mit unterschiedlicher Endung:

```
Struktur_<Ordnername>_<JJJJ-MM-TT>_<HHMMSS>.txt
Struktur_<Ordnername>_<JJJJ-MM-TT>_<HHMMSS>.json
```

- Ungültige Zeichen im Ordnernamen werden durch `_` ersetzt.
- Bei einer **Laufwerkswurzel** (z. B. `D:\`) heißt der Namensteil `Laufwerk_D`.
- Sind keine Zeichen übrig, wird `Ordner` verwendet.

### TXT

- Codierung UTF-8 mit BOM, Zeilenende CRLF.
- Kopfzeilen mit Quellordner, Erstellungszeitpunkt und Legende (Ordner enden mit `\`).
- Baumdarstellung mit `├──`, `└──` und `│`.
- Verknüpfungen erscheinen als `Name\  [Verknüpfung - nicht verfolgt]`.
- Ordner ohne Zugriff erscheinen mit `[FEHLER: …]`.
- Am Ende steht eine Zusammenfassung mit den Anzahlen.

### JSON

- Codierung UTF-8 ohne BOM, Zeilenende LF.
- Aufbau:

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

| Feld | Bedeutung |
|---|---|
| `type` | `folder`, `file` oder `link` |
| `name` | Name des Eintrags |
| `path` | Pfad **relativ zum Quellordner**, mit `\` als Trenner. Beim Wurzelordner leer |
| `children` | Inhalt eines Ordners |
| `error` | Nur bei Ordnern, die nicht gelesen werden konnten: Fehlermeldung |
| `summary` | Anzahl der Ordner, Dateien, nicht verfolgten Verknüpfungen und Fehler |

---

## Verhalten im Detail

- **Sortierung:** Zuerst alle Ordner, dann alle Dateien, jeweils nach Name ohne Beachtung der Groß-/Kleinschreibung.
- **Verknüpfungen:** Ordner, die Junctions oder symbolische Links sind (Attribut „Reparse Point“), werden aufgeführt, aber **nicht betreten**. Sie zählen unter „Verknüpfungen“.
- **Zugriffsfehler:** Kann ein Ordner nicht gelesen werden (z. B. „Zugriff verweigert“), wird er mit der Fehlermeldung markiert und gezählt. Der Export läuft weiter. Bei Fehlern empfiehlt das Programm, es als Administrator zu starten.
- **Abbruch oder Fehler:** Bricht der Export ab oder tritt ein schwerer Fehler auf, werden die unvollständigen TXT- und JSON-Dateien **gelöscht**.
- **Schließen:** Solange ein Export läuft, lässt sich das Fenster nicht schließen. Zuerst muss abgebrochen werden.
- **Zählung „Ordner“:** Der Quellordner selbst wird mitgezählt.
- **Speicherbedarf:** Das Programm schreibt während des Durchlaufs direkt in die Dateien und hält nicht den ganzen Baum im Speicher. Die Ordner eines einzelnen Verzeichnisses werden zum Sortieren kurz in Listen gehalten.

---

## Hinweise und Einschränkungen

- **Nur Namen:** Größen, Datumsangaben, Attribute und Dateiinhalte werden nicht ausgegeben.
- **Kein Filter:** Der Code blendet keine Dateien oder Ordner aus, auch nicht versteckte oder Systemdateien, soweit der Benutzer sie lesen darf.
- **Zielordner innerhalb des Quellordners:** Der Code schließt den Zielordner nicht aus. Liegt er im Quellordner, könnten die Ausgabedateien in der Liste erscheinen. Das habe ich nicht getestet. Am besten einen Zielordner außerhalb des Quellordners wählen.
- **Dateien als Link:** Als Verknüpfung gezählt werden nur **Ordner** mit dem Attribut „Reparse Point“. Verknüpfungen auf Dateien werden wie Dateien aufgelistet.
- **Lange Pfade:** Manifest und Konfigurationsdatei aktivieren die Unterstützung langer Pfade. Ob sie auf deinem System tatsächlich greift, hängt von der Windows-Konfiguration ab und wurde hier nicht geprüft.
- **Nur Lesezugriff:** Das Programm schreibt nur die beiden Ausgabedateien im Zielordner.

---

## Fehlerbehebung

| Problem | Mögliche Ursache / Lösung |
|---|---|
| `BUILD FEHLGESCHLAGEN` | Meldungen mit `error CS…` über der Zeile lesen. Prüfen, ob `FolderList.cs` und `app.manifest` im selben Ordner liegen |
| „csc.exe nicht gefunden“ | .NET Framework 4.x (64 Bit) ist nicht vorhanden oder liegt woanders |
| Umlaute im Programm falsch | `FolderList.cs` muss als UTF-8 gespeichert sein. Das Build-Skript liest mit `/codepage:65001` |
| Programm startet nicht oder meldet Konfigurationsfehler | Liegt `FolderList.exe.config` neben der EXE und heißt genau so? |
| „Quellordner existiert nicht“ / „Zielordner existiert nicht“ | Pfade prüfen. Beide Ordner müssen vorhanden sein |
| Im Ergebnis stehen `[FEHLER: …]`-Einträge | Der Benutzer darf diese Ordner nicht lesen. Ggf. als Administrator starten |
| Fenster lässt sich nicht schließen | Ein Export läuft noch. Zuerst **Abbrechen** |

---


