# Overview

This project is under development and will massively change during the course of its evolution.

![](nothing-to-see-here.gif)

ItchyPassword is a web-based password manager, that should be usable for anyone.<br/>
It works fully offline with all the cryptography happening in the browser. It requires a browser with SubtleCrypto API available.

Works fine with Chromium-based browsers (Edge, Chrome) on desktop and mobile (Android).

There is the possibility to plug vault storages to automatically fetch and store data from/to external services, so this will require an internet access, but it is optional and those actions (fetch/store) can be performed manually instead.

## Web app

You can find a usable version of the application [here](https://tanukisharp.github.io/ItchyPassword).

### Build

You need to have NPM installed to fetch packages. NodeJS is useless for this project.<br/>
Tested with NPM 6.0.0 and 6.13.0.

```sh
cd web
npm install
npm run build # or npm run watch, or npm run build-dev
```

### Run and use

Open `docs/index.html` in your browser.
