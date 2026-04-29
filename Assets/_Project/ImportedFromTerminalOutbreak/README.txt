Innhold kopiert fra prosjektet «TerminalOutbreak» (EksamenUnity/TerminalOutbreak):
- SimplePoly City – Low Poly Assets (trunkert: kun stein/benk/busk/gran + Materials som Level01-brønnen trenger).

Fjernet: Vehicles, Roads, Buildings-prefabs og øvrige FBX i Models (Unity 6 ga «self-intersecting polygon»-advarsler på en del kjøretøy/vei-meshes ved import).
Tomme Prefab-mapper + .meta for Buildings/Roads/Vehicles er også fjernet (unngår «meta exists but folder can't be found»).

Hvis konsollen fortsatt viser gamle mesh-advarsler etter dette: lukk Unity, slett mappa Library i prosjektroten (Unity bygger den på nytt), eller høyreklikk ImportedFromTerminalOutbreak → Reimport All.

Bruk: dekor i ExamUnityGame (Exam → Greybox → Level01).

Lisens: følg SimplePoly-/butikk-vilkår som fulgte originalpakken i TerminalOutbreak-prosjektet.
