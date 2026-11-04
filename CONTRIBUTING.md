# Zusammenarbeit

## Tagesablauf

Vor einer Aufgabe Arbeit sichern und prüfen, dass der Arbeitsbaum sauber ist.

```powershell
git switch main
git pull --ff-only
git switch -c feature/first-person-controller
# Änderungen implementieren und prüfen
git status
git diff
git add <bewusst-ausgewaehlte-Dateien>
git diff --cached
git commit -m "feat: add radial player movement"
git push -u origin feature/first-person-controller
```

Auf GitHub Pull Request nach main öffnen, Testnachweise ergänzen und den anderen Entwickler
um Review bitten. Nach Freigabe mergen. Für Fehler fix/terrain-collision, für Dokumentation
docs/game-design verwenden. Nicht gleichzeitig im selben lokalen Checkout arbeiten.

## Konflikte

Vor Beginn Zuständigkeiten für Szenen, Prefabs und ProjectSettings abstimmen.
Kleine Prefabs und getrennte Test-Szenen bevorzugen. Assets mitsamt .meta verschieben.
Bei Konflikten zuerst sichern/committen, dann main in den Feature-Branch mergen.
Kein pauschales ours/theirs für Unity-YAML. GUID-Bezüge, fehlende Komponenten und Referenzen
im Editor überprüfen, danach kompilieren und testen.

UnityYAMLMerge wird über .gitattributes für .unity/.prefab vorbereitet und lokal konfiguriert.
Die Einrichtung für den zweiten PC steht in SETUP.md. Ein semantisch erfolgreicher Merge
ersetzt keine Kontrolle im Editor. Binärkonflikte gemeinsam entscheiden, nicht manuell editieren.

## Git LFS

FBX, Blend, PSD, TGA, WAV, MP4 und EXR sind LFS-Dateien. Kleine PNG bleiben normales Git.
Für große einzelne PNG vor dem ersten Commit `git lfs track "Assets/.../LargeTexture.png"`
verwenden und .gitattributes mitcommitten. Team-Richtwert für eine Prüfung: 10 MiB.
Keine automatische Migration vorhandener Historie. Unity-YAML und Meta bleiben Text.

Jede neue Binärversion benötigt erneut Speicher; Downloads verbrauchen Bandbreite des
Repositorybesitzers. Das gilt auch bei öffentlichen Repositories und CI.
Aktuelle Kontingente und Kosten im GitHub-Konto unter Billing prüfen, vor großen Uploads
ein Budget festlegen. Ein Nullbudget kann weitere LFS-Nutzung bei Überschreitung blockieren.
Quelle: https://docs.github.com/en/billing/concepts/product-billing/git-lfs

## Collaborator und Schutz von main

Besitzer: Repository > Settings > Collaborators > Add people. Exakten GitHub-Namen des
Freundes auswählen; Einladung muss angenommen werden. Keine unbekannten Konten einladen.

Nach dem initialen Push: Settings > Rules > Rulesets > New branch ruleset, Ziel main,
Enforcement Active. Pull Request verlangen, mindestens eine Freigabe, alte Freigaben bei
Änderungen verwerfen, offene Diskussionen auflösen, Force-Push und Branch-Löschung sperren.
Den Check **Repository integrity** nach seinem ersten erfolgreichen Lauf als erforderlich
auswählen. Bei zwei Entwicklern soll der jeweils andere prüfen; keine nicht verfügbare
zusätzliche Review-Anzahl verlangen. Regeln sind dokumentiert und werden nicht als aktiv
behauptet, solange sie nicht tatsächlich eingerichtet sind.
