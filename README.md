# MagicGame

## GitHub Pages playtest

The Pages landing screen provides separate **Battle** and **Overworld** WebGL builds. The GitHub Actions workflow builds `SpellrightCombat` and `OverworldTest`, then deploys the `docs` directory when changes are pushed to `main`.

To enable deployment, set the repository's Pages source to **GitHub Actions** in **Settings → Pages**. Add the Unity licensing secrets required by GameCI (`UNITY_LICENSE`; and `UNITY_EMAIL` / `UNITY_PASSWORD` if using account-based activation). The first workflow run builds both WebGL players; the landing page is then published at `https://ejones81973.github.io/MagicGame/`.
