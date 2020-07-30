import { getElementById } from './ui';
import { rootComponent } from './components/rootComponent';

declare const COMMITHASH: string;

const version = COMMITHASH.substr(0, 11);
const githubLink = '<a href="https://github.com/TanukiSharp/ItchyPassword" target="_blank">github</a>';

getElementById('divInfo').innerHTML = `${version}<br/>${githubLink}`;

rootComponent.init();
