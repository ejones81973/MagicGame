# MagicGame

## GitHub Pages playtest

The Pages landing screen offers separate **Battle** and **Overworld** WebGL builds. GitHub Actions builds `SpellrightCombat` and `OverworldTest`, then deploys the `docs` directory when changes are pushed to `main`.

To enable deployment, set the repository Pages source to **GitHub Actions** in **Settings → Pages**. Add Unity license credentials in **Settings → Secrets and variables → Actions**:

- Unity Personal: add `UNITY_LICENSE` with the contents of your activated `.ulf` license file, plus `UNITY_EMAIL` and `UNITY_PASSWORD` for the associated Unity account.
- Unity Pro/Plus: add `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.

The workflow supports both license types. The first successful run builds both WebGL players; the landing page is then published at `https://ejones81973.github.io/MagicGame/`.
