# ExamUnityGame

Et selvstendig 3D Unity-prosjekt med tre baner, laget for å øve på grunnleggende spillprogrammering (spillerkontroll, fiende-AI, spilltilstand) som en del av forberedelsene til **PG2202 Spillprogrammering**. Ikke en eksamensinnlevering — se [`CartoonZombiesEksamen`](https://github.com/lamar0112/CartoonZombiesEksamen) for den faktiske eksamensbesvarelsen.

Nivåene er bygget som "greybox"-nivåer (enkel geometri før detaljering), et vanlig steg i leveldesign-prosessen for å teste layout og spillbarhet før man legger til endelig visuelt innhold.

## Funksjonalitet

- **Tre baner** pluss en hovedmeny, lastet via `GameManager`s scene-håndtering.
- **Spillerkontroll** og en egen kamera-oppfølger (`CameraFollow`) med mus-styrt vinkel.
- **Fiende-AI:** tilstandsmaskin-basert (samme mønster som i `UnityMario`).
- **Spilltilstand:** singleton `GameManager` som holder styr på poeng, samlede objekter og pause på tvers av scener (`DontDestroyOnLoad`).
- **UI-robusthet:** automatisk tildeling av standard TextMeshPro-font for å unngå manglende font-referanser i UI-et.
- Bydekor importert og renset fra et lavpoly-assetpakke ("SimplePoly City") for et av nivåene.

## Teknologier

- Unity (URP)
- C# — egne scripts for spillerkontroll, kamera, fiende-AI og spilltilstand
- TextMesh Pro for UI
- Delvis lavpoly by-assets (kuratert utvalg, tunge/problematiske meshes fjernet)

## Hva jeg lærte

- Bygge og teste flere nivåer med greybox-metodikk før visuell polering.
- Robust UI-oppsett (font-fallback) som unngår vanlige TextMeshPro-fallgruver.
- Kuratere og rense importerte assetpakker — fjerne meshes som ga importfeil i stedet for å bruke pakken ukritisk.
- Videreutvikle samme AI- og spilltilstand-mønster som i `UnityMario`, som en del av en gradvis læringsprosess mot PG2202-eksamenen.

## Kjøre prosjektet lokalt

1. Åpne prosjektet i Unity Hub (URP-prosjekt).
2. Åpne `Assets/_Project/Scenes/MainMenu.unity`.
3. Trykk Play.
