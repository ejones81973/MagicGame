# MagicGame

## GitHub Pages playtest

The Pages site offers separate **Battle** and **Overworld** WebGL builds. Unity builds the games locally, and GitHub Actions deploys the already-built files from `docs` without running Unity or requiring Unity credentials in GitHub.

### Build and publish

1. Open the project in Unity `6000.5.2f1` with the WebGL Build Support module installed and your local Unity Editor activated.
2. In Unity, select **Spellright → Build Pages WebGL (Battle + Overworld)**. This creates `docs/battle` and `docs/overworld`.
3. Commit and push the generated build folders along with any landing-page changes.
4. Set the repository Pages source to **GitHub Actions** under **Settings → Pages**. Push to `main` to deploy, or manually run **Build and deploy GitHub Pages** from the Actions tab.

Unity license files and account credentials stay on the local machine. The Pages workflow only checks for the two built `index.html` files, uploads `docs`, and deploys it. The site URL is `https://ejones81973.github.io/MagicGame/`.
