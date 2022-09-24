(()=>{"use strict";const t="#FFD0D0";function e(t){const e=document.getElementById(t);if(null===t)throw new Error(`DOM element '${t}' not found.`);return e}async function n(t){try{return await navigator.clipboard.writeText(t),!0}catch(t){const e=t;return console.error(e.stack||t),!1}}function r(t,e=!1){t.value="",e&&t.focus()}function i(t,e){const n=function(t,e){let n;return{start:()=>{void 0!==n&&(clearTimeout(n),n=void 0),t()},end:()=>{void 0!==n&&clearTimeout(n),n=setTimeout(t,1e3)}}}((()=>{t.classList.remove("good-flash"),t.classList.remove("bad-flash")})),r=async()=>{t.disabled=!0,n.start();try{const r=e();let i;i=r instanceof Promise?await r:r,void 0===i||!0===i?t.classList.add("good-flash"):t.classList.add("bad-flash")}catch(e){const n=e;t.classList.add("bad-flash"),console.error(n.stack||e)}finally{n.end(),t.disabled=!1}};return t.addEventListener("click",r),r}function o(t,e){return i(e,(()=>n(t.value)))}function a(t,e){e?t.style.removeProperty("display"):t.style.setProperty("display","none")}function s(t,e){for(const n of t)a(n,e)}class l{tabs;_activeTabIndex=-1;get activeTabIndex(){return this._activeTabIndex}set activeTabIndex(t){if(t<0||t>=this.tabs.length)throw new Error(`Argument 'index' out of range. Must be in range [0;${this.tabs.length-1}].`);this.setActiveTab(t)}constructor(t){this.tabs=t;for(let e=0;e<this.tabs.length;e+=1)t[e].getTabButton().addEventListener("click",(()=>{this.setActiveTab(e)}));this.setActiveTab(0)}setActiveTab(t){if(t===this._activeTabIndex)return;let e;for(e of this.tabs){const t=e.getTabButton();t.style.removeProperty("font-weight"),t.style.setProperty("color","#C0C0C0"),e.getTabContent().style.setProperty("display","none")}const n=this.tabs[t].getTabButton();n.style.setProperty("font-weight","bold"),n.style.removeProperty("color"),this.tabs[t].getTabContent().style.removeProperty("display"),this._activeTabIndex=t,this.tabs[t].onTabSelected()}}const u="Stores the string in memory and removes it from the UI component. Prevents a physical intruder from copy/pasting the value.",c=e("txtPrivatePart"),h=e("txtPrivatePartConfirmation"),d=e("btnProtect"),p=e("spnProtectedConfirmation"),f=e("spnPrivatePartSize"),g=e("spnPrivatePartSizeConfirmation");let m;const w=[];function y(){return void 0!==m?m:c.value}function b(){0!==c.value.length&&(m=c.value,p.innerHTML="Protected",r(c),r(h),f.innerHTML="0",g.innerHTML="0",c.disabled=!0,h.disabled=!0,d.innerHTML="Clear and unlock",d.title="Removes the string form memory and re-enables the UI component.",C())}function v(){void 0===m?b():(m=void 0,p.innerHTML="",c.disabled=!1,h.disabled=!1,d.innerHTML="Protect and lock",d.title=u,d.disabled=!0)}const E=new class{action;delay;timeout;constructor(t,e){this.action=t,this.delay=e}reset(t){void 0!==this.timeout&&clearTimeout(this.timeout);const e=void 0!==t?t:this.delay;this.timeout=window.setTimeout((()=>{this.action(),this.timeout=void 0}),e)}}(b,6e4);function T(){let t;for(t of(d.disabled=0===c.value.length,f.innerHTML=c.value.length.toString(),C(),w))t();E.reset()}function C(){h.value===c.value?h.style.setProperty("background","#D0FFD0"):h.style.setProperty("background",t)}function S(){g.innerHTML=h.value.length.toString(),E.reset(),C()}function A(t,e){const n=BigInt(e.length);let r="",i=function(t){const e=t.byteLength,n=new DataView(t,0);let r=0n,i=1n;for(let t=0;t<e;t+=1)r+=BigInt(n.getUint8(t))*i,i*=256n;return r}(t);for(;i>0n;){const t=i%n;i/=n,r+=e[BigInt.asUintN(8,t)]}return r}class L extends Error{_name;get name(){return this._name}constructor(t){super(t),this._name=L.ERROR_NAME,Object.setPrototypeOf(this,new.target.prototype)}static ERROR_NAME="TaskCancelledError";static isMatching(t){return t&&t.name===L.ERROR_NAME}}class P{_isCancelled=!1;_token;constructor(){this._token=new O(this)}get isCancelled(){return this._isCancelled}get token(){return this._token}cancel(){this._isCancelled=!0}}class O{source;static _none=null;static get none(){return null===O._none&&(O._none=new O(new P)),O._none}constructor(t){this.source=t}get isCancelled(){return this.source.isCancelled}}function _(t){if(t.isCancelled)throw new L}const V="abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";async function R(t,e,n){const r=await window.crypto.subtle.importKey("raw",t,"PBKDF2",!1,["deriveKey"]);_(n);const i={name:"PBKDF2",hash:"SHA-512",iterations:1e5,salt:e},o=await window.crypto.subtle.deriveKey(i,r,{name:"AES-GCM",length:256},!0,["encrypt"]);_(n);const a=await window.crypto.subtle.exportKey("raw",o);return _(n),a}function k(t,e){return t.length<=e?t:t.substring(0,e)}function N(t){return(new TextEncoder).encode(t).buffer}function I(t){return null!=t&&!1===t.hasOwnProperty("constructor")&&"Object"===t.constructor.name}function x(t){const e={};for(const[n,r]of Object.entries(t).sort(((t,e)=>t[0].localeCompare(e[0]))))e[n]=I(r)?x(r):r;return e}const H={};function M(t){if(!t)throw new TypeError("Argument 'serviceName' is mandatory.");const e=H[t];if(void 0===e)throw new Error(`Service '${t}' is not registered.`);return e}function U(t,e){if(!t)throw new TypeError("Argument 'serviceName' is mandatory.");if(void 0===e)throw new TypeError("Argument 'instance' cannot be undefined.");if(void 0!==H[t])throw new Error(`Service '${t}' is already registered.`);H[t]=e}class ${secureLocalStorage;static BASE_URL="https://api.github.com";static AUTH_TOKEN_KEY_NAME="GitHubVaultStorageBase.AuthToken";token=null;oneTimePassword=null;currentVaultContentHash=null;username=null;repositoryName=null;vaultFilename=null;static LOCAL_STORAGE_KEY_USERNAME="GitHubVaultStorageBase.Username";static LOCAL_STORAGE_KEY_REPO="GitHubVaultStorageBase.Repository";static LOCAL_STORAGE_KEY_FILENAME="GitHubVaultStorageBase.Filename";getUsername(){return this.username}getRepositoryName(){return this.repositoryName}getVaultFilename(){return this.vaultFilename}constructor(t){this.secureLocalStorage=t}clear(){this.secureLocalStorage.removeItem(B.LOCAL_STORAGE_KEY_USERNAME),this.secureLocalStorage.removeItem(B.LOCAL_STORAGE_KEY_REPO),this.secureLocalStorage.removeItem(B.LOCAL_STORAGE_KEY_FILENAME),this.secureLocalStorage.removeItem($.AUTH_TOKEN_KEY_NAME)}constructTokenAuthString(){return`token ${this.token}`}constructFetchRequest(t,e,n){const r={Accept:"application/vnd.github.v3+json","Content-Type":"application/json",Authorization:e};return this.oneTimePassword&&(r["x-github-otp"]=this.oneTimePassword),{method:t,headers:r,body:n?JSON.stringify(n):void 0}}constructUrl(t){return`${$.BASE_URL}${t}`}async request(t,e,n,r,i){const o=this.constructUrl(n),a=this.constructFetchRequest(e,r,i);let s=await fetch(o,a);return 401===s.status&&t?(this.oneTimePassword=prompt("Input your 2FA code:"),this.oneTimePassword?await this.request(t,e,n,r,i):null):s}getSetVaultParameter(t,e,n){let r=window.localStorage.getItem(t);return r||(r=prompt(e,n),r?(window.localStorage.setItem(t,r),r):null)}ensureVaultParameters(){const t=new URL(window.location.toString());let e="",n="";if("github.com"===t.hostname){const r=t.pathname.split("/");r.length>=3&&(e=r[1],n=`${r[2]}Vault`)}const r=this.getSetVaultParameter($.LOCAL_STORAGE_KEY_USERNAME,"GitHub account username:",e);if(!r)return Promise.resolve(!1);this.username=r;const i=this.getSetVaultParameter($.LOCAL_STORAGE_KEY_REPO,"Vault GitHub repository name:",n);if(!i)return Promise.resolve(!1);this.repositoryName=i;const o=this.getSetVaultParameter($.LOCAL_STORAGE_KEY_FILENAME,"Vault filename:","vault.json");return o?(this.vaultFilename=o,Promise.resolve(!0)):Promise.resolve(!1)}async ensureToken(){let t=await this.secureLocalStorage.getItem($.AUTH_TOKEN_KEY_NAME);return null===t&&(t=await this.getToken()),!!t&&(await this.secureLocalStorage.setItem($.AUTH_TOKEN_KEY_NAME,t),this.token=t,!0)}constructVaultFileUrl(){return`/repos/${this.username}/${this.repositoryName}/contents/${this.vaultFilename}`}async getVaultContent(){if(!1===await this.ensureVaultParameters())return null;if(!1===await this.ensureToken())return null;const t=this.constructVaultFileUrl(),e=await this.request(!1,"GET",t,this.constructTokenAuthString());if(null===e)return console.warn("Fetching vault content aborted."),null;if(!1===e.ok)return 401===e.status?(this.secureLocalStorage.removeItem($.AUTH_TOKEN_KEY_NAME),this.token=null,this.oneTimePassword=null,await this.getVaultContent()):404===e.status?await this.setVaultContent("{}","[ItchyPassword] Creation of vault file")?"{}":null:(console.error(`Failed to fetch vault file '${this.vaultFilename}'.`,e),null);const n=await e.json();this.currentVaultContentHash=n.sha;const r=atob(n.content);return""===r.trim()?"{}":r}async setVaultContent(t,e){if(!1===await this.ensureVaultParameters())return!1;if(!1===await this.ensureToken())return!1;const n={message:e,content:btoa(t),sha:this.currentVaultContentHash},r=this.constructVaultFileUrl(),i=await this.request(!1,"PUT",r,this.constructTokenAuthString(),n);if(null===i)return console.warn("Push new vault content aborted."),!1;const o=await i.json();return!1===i.ok?(console.error(`Failed to create/update vault file '${this.vaultFilename}'.`,i,o),!1):(this.currentVaultContentHash=o.content.sha,!0)}}class K extends ${getToken(){const t=prompt("Personal access token:");return Promise.resolve(t)}}class B extends ${static AUTHORIZATION_NAME="github.com/TanukiSharp/ItchyPassword";static LOCAL_STORAGE_KEY_PASSWORD_PUBLIC="GitHubApiVaultStorage.PasswordPublicPart";static LOCAL_STORAGE_KEY_PASSWORD_LENGTH="GitHubApiVaultStorage.PasswordLength";static LOCAL_STORAGE_KEY_BROWSER_NAME="GitHubApiVaultStorage.BrowserName";basicAuthHeader=null;authorizationName=null;constructor(t){super(t)}clear(){super.clear(),this.secureLocalStorage.removeItem(B.LOCAL_STORAGE_KEY_PASSWORD_PUBLIC),this.secureLocalStorage.removeItem(B.LOCAL_STORAGE_KEY_PASSWORD_LENGTH),this.secureLocalStorage.removeItem(B.LOCAL_STORAGE_KEY_BROWSER_NAME)}constructBasicAuthString(t,e){return console.log("username:",t),console.log("password:",e),`Basic ${btoa(`${t}:${e}`)}`}async listAuthorizations(){if(!this.basicAuthHeader)return null;const t=await this.request(!0,"GET","/authorizations",this.basicAuthHeader);return null===t?(console.warn("List authorizations aborted."),null):!1===t.ok?(console.error("Failed to list authorizations.",t),null):await t.json()}async deleteAuthorization(t){if(!this.basicAuthHeader)return!1;const e=await this.request(!0,"DELETE",`/authorizations/${t.id}`,this.basicAuthHeader);return null===e?(console.warn("Delete authorization aborted."),!1):(!1===e.ok&&console.error(`Failed to delete authorization '${t.id}'.`,e),e.ok)}async createAuthorization(){if(!this.authorizationName)return null;if(!this.basicAuthHeader)return null;const t={scopes:["repo"],note:this.authorizationName},e=await this.request(!0,"POST","/authorizations",this.basicAuthHeader,t);return null===e?(console.warn("Create new authorization aborted."),null):!1===e.ok?(console.error("Failed to create new authorization.",e),null):(await e.json()).token}findAuthorization(t){if(!this.authorizationName)return null;for(const e of t)if(e.app&&e.app.name===this.authorizationName)return e;return null}async ensureVaultParameters(){if(!1===await super.ensureVaultParameters())return!1;const t=this.getUsername();if(!t)return!1;const e=this.getSetVaultParameter(B.LOCAL_STORAGE_KEY_PASSWORD_PUBLIC,"GitHub account password public part:");if(!e)return!1;const n=this.getSetVaultParameter(B.LOCAL_STORAGE_KEY_PASSWORD_LENGTH,"GitHub account password length:");if(!n)return!1;const r=parseInt(n,10);if(!1===Number.isSafeInteger(r)||r<=0)return!1;let i=await Pe(e,le,O.none);if(!i)return!1;this.basicAuthHeader=this.constructBasicAuthString(t,i.substring(0,r));const o=this.getSetVaultParameter(B.LOCAL_STORAGE_KEY_BROWSER_NAME,"Current device/browser name:");return!!o&&(this.authorizationName=`${B.AUTHORIZATION_NAME} (${o})`,!0)}async getToken(){const t=await this.listAuthorizations();if(null===t)return null;const e=this.findAuthorization(t);return null!==e&&!1===await this.deleteAuthorization(e)?null:await this.createAuthorization()}}class G{get version(){return 2}get description(){return"PBKDF2 + AES-GCM"}async encrypt(t,e,n){const r=new ArrayBuffer(44+t.byteLength),i=crypto.getRandomValues(new Uint8Array(r,0,12)),o=crypto.getRandomValues(new Uint8Array(r,12,16)),a={name:"AES-GCM",iv:i},s=await window.crypto.subtle.importKey("raw",await R(e,o,n),{name:"AES-GCM",length:256},!1,["encrypt"]);_(n);const l=await window.crypto.subtle.encrypt(a,s,t);return _(n),new Uint8Array(r).set(new Uint8Array(l),28),r}async decrypt(t,e,n){const r=new Uint8Array(t,0,12),i=new Uint8Array(t,12,16),o=new Uint8Array(t,28),a={name:"AES-GCM",iv:r},s=await R(e,i,n);_(n);const l=await window.crypto.subtle.importKey("raw",s,{name:"AES-GCM",length:256},!1,["decrypt"]);_(n);const u=await window.crypto.subtle.decrypt(a,l,o);return _(n),u}}class F{cipherComponent;constructor(t){this.cipherComponent=t}async activate(t,e,n){return!1!==await this.cipherComponent.setParameters(e,n,t)&&(this.cipherComponent.getTabButton().click(),!0)}}const D=e("btnTabCiphers"),j=e("divTabCiphers"),z=new G,Y=e("btnClearAllCipherInfo"),W=e("txtCipherName"),q=e("txtCipherSource"),J=e("txtCipherTarget"),Z=e("btnEncrypt"),X=e("btnDecrypt"),Q=e("btnCopyCipherSource"),tt=e("btnClearCipherSource"),et=e("btnCopyCipherTarget"),nt=e("btnClearCipherTarget");let rt;function it(){q.style.removeProperty("background-color")}function ot(){q.style.setProperty("background-color",t)}function at(){it(),J.style.removeProperty("background-color")}function st(){rt=void 0}function lt(t,e){const n=t.length>0&&J.value!==t;J.value=t,n&&e?rt=(new Date).toISOString():st(),ut()}function ut(){""!==J.value&&""!==W.value?Nt({datetime:rt,version:z.version,value:J.value},`ciphers/${W.value}`):kt()}async function ct(t,e){const n=y();if(0===n.length)return console.warn("Private part is empty"),null;const r=N(t),i=N(n),o=await z.encrypt(r,i,e);return _(e),function(t,e){const n=BigInt(e.length);let r="",i=function(t){t=function(t){if(t.byteLength>65535)throw new Error(`Buffer too large: ${t.byteLength} bytes`);let e=t.byteLength;const n=new Uint8Array(2+t.byteLength);for(let t=0;t<2;t+=1)n[t]=e%256,e/=256;return n.set(new Uint8Array(t),2),n.buffer}(t);const e=t.byteLength,n=new DataView(t,0);let r=0n,i=1n;for(let t=0;t<e;t+=1)r+=BigInt(n.getUint8(t))*i,i*=256n;return r}(t);for(;i>0n;){const t=i%n;i/=n,r+=e[BigInt.asUintN(8,t)]}return r}(o,V)}async function ht(t,e){const n=y();if(0===n.length)return console.warn("Private part is empty"),null;try{const i=function(t,e){const n=BigInt(e.length);let r=0n,i=1n;for(let o=0;o<t.length;o+=1)r+=BigInt(e.indexOf(t[o]))*i,i*=n;return function(t){const e=[];for(;t>0n;){const n=t%256n;t/=256n;const r=Number(BigInt.asUintN(8,n));e.push(r)}let n=e[0];e.length>1&&(n+=256*e[1]);const r=n-(e.length-2);for(let t=0;t<r;t+=1)e.push(0);return new Uint8Array(e.slice(2)).buffer}(r)}(t,V),o=N(n),a=await z.decrypt(i,o,e);return _(e),r=a,(new TextDecoder).decode(r)}catch(t){const e=t;return function(t){if(L.isMatching(t))throw t}(e),console.warn("Failed to decrypt"+(e.message?`, error: ${e.message}`:", no error message")),null}var r}async function dt(){if(q.focus(),lt("",!0),at(),0===q.value.length)return ot(),!1;const t=await ct(q.value,O.none);return null!==t&&(lt(t,!0),ut(),!0)}async function pt(){if(q.focus(),lt("",!1),at(),0===q.value.length)return ot(),!1;const e=await ht(q.value,O.none);return null===e?(J.style.setProperty("background-color",t),!1):(lt(e,!1),!0)}class ft{name="Cipher";getTabButton(){return D}getTabContent(){return j}onTabSelected(){Ht(),ut(),W.focus()}static fullPathToStoragePath(t,e){const n="<root>/",r=`/ciphers/${e}`;return!1===t.startsWith(n)||!1===t.endsWith(r)?null:t.substring(n.length,t.length-r.length)}async setParameters(t,e,n){W.value="",q.value="",J.value="",It(""),xt("");const r=await ht(e.value,O.none);if(null===r)return alert(`Failed to decrypt cipher '${t}'.`),!1;const i=ft.fullPathToStoragePath(n,t);return null===i?(console.error(`Failed to retrieve storage path from full path '${n}'.`),alert("Failed to retrieve storage path from full path."),!1):(e.customKeys&&xt(JSON.stringify(e.customKeys,null,4)),delete e.customKeys,W.value=t,q.value=r,It(i),Nt(e,`ciphers/${t}`),!0)}getVaultHint(){return`${this.name.toLowerCase()} '${W.value}'`}init(){o(q,Q),o(J,et),i(Z,dt),i(X,pt),W.addEventListener("input",(()=>{ut()})),q.addEventListener("input",(()=>{q.value.length>0&&it()})),Y.addEventListener("click",(()=>{W.value="",q.value="",J.value="",st(),at(),wt.value="",bt.value="",Et.value="",Tt=void 0,Ct=void 0,Ot(!0)})),tt.addEventListener("click",(()=>{r(q,!0)})),nt.addEventListener("click",(()=>{lt("",!1)})),U("cipher",new F(this))}}class gt{get length(){return window.localStorage.length}clear(){window.localStorage.clear()}key(t){return window.localStorage.key(t)}removeItem(t){window.localStorage.removeItem(t)}async getItem(t){const e=window.localStorage.getItem(t);return null===e?null:await ht(e,O.none)}async setItem(t,e){const n=await ct(e,O.none);null!==n?window.localStorage.setItem(t,n):console.error("Failed to encrypt value. (nothing stored)")}}const mt=e("divStorageOutput"),wt=e("txtPath"),yt=e("lblMatchingPath"),bt=e("txtParameters"),vt=e("btnPushToVault"),Et=e("txtCustomKeys");let Tt,Ct,St=new K(new gt);function At(t,e){const n=t.indexOf("/"),r={},i=n>=0?t.substring(0,n):t,o=n>=0?t.substring(n+1):void 0;if(void 0===e){const t={};t[i]=r,e={head:t,tailParent:t,tail:r}}else e.tail[i]=r,e.tailParent=e.tail,e.tail=r;return o?At(o,e):e}function Lt(){!function(){const t=M("vault").computeUserPathMatchDepth(wt.value);if(t>0){const e=function(t,e){let n=0;for(let r=0;r<e;r+=1){if(n=t.indexOf("/",n),n<0){n=t.length+1;break}n+=1}return t.substring(0,n-1)}(wt.value,t);yt.innerText=e}else yt.innerText=""}(),_t()}function Pt(){_t()}function Ot(e){e?Et.style.removeProperty("background"):Et.style.setProperty("background",t)}function _t(){if(void 0===Tt||void 0===Ct)return;const t=At(`${wt.value}/${Ct}`),e=t.tail;for(const[t,n]of Object.entries(Tt))e[t]=n;const n=function(){if(""===Et.value)return null;try{const t=JSON.parse(Et.value);return null===t||"Object"!==t.constructor.name?null:t}catch{return null}}();Ot(""===Et.value||null!==n),null!==n&&(e.customKeys=n),0===Object.keys(e).length&&(t.tailParent[Object.keys(t.tailParent)[0]]=null),bt.value=JSON.stringify(x(t.head),void 0,4)}function Vt(t,e){for(const n of Object.keys(t)){const r=e[n],i=t[n];null!=r&&"Object"===r.constructor.name&&"Object"===i.constructor.name?Vt(i,r):e[n]=i}}async function Rt(){const t=await St.getVaultContent();if(null===t)return!1;const e=JSON.parse(bt.value);let n=JSON.parse(t);Vt(e,n);const r=function(){const t=Pn.getActiveComponent();if(null===t)throw new Error("Could not determine active component.");let e=t.getVaultHint();const n=yt.innerText,r=wt.value;return n?n===r?`Updated ${e} for '${r}'`:`Updated ${e} for '${n}' adding '${function(t,...e){const n=function(t,e){for(let n=0;n<t.length;n+=1)if(!1===e.includes(t[n]))return n;return t.length}(t,e),r=function(t,e){for(let n=t.length-1;n>=0;n-=1)if(!1===e.includes(t[n]))return n+1;return t.length}(t,e);return t.substring(n,r)}(r.substring(n.length),"/")}'`:`Added ${e} for '${r}'`}(),i=JSON.stringify(x(n),void 0,4)+"\n";return await St.setVaultContent(i,`[ItchyPassword] ${r}`),!0}function kt(){Tt=void 0,Ct=void 0,r(bt)}function Nt(t,e){Tt=t,Ct=e,_t()}function It(t){wt.value=t,Lt()}function xt(t){Et.value=t}function Ht(){mt.style.setProperty("display","initial")}function Mt(){mt.style.setProperty("display","none")}class Ut{async generateAndCopyPasswordToClipboard(t,e,r){e=void 0!==e?e:le,r=void 0!==r?r:se;const i=await Pe(t,e,O.none);if(null===i)return!1;const o=k(i,Math.max(4,r));return await n(o)}}const $t=e("btnTabPasswords"),Kt=e("divTabPasswords"),Bt=new class{hkdfPurpose;_description;constructor(t){this.hkdfPurpose=N(t),this._description=`HKDF(PBKDF2, HMAC512) [purpose: ${t}]`}get version(){return 1}get description(){return this._description}async generatePassword(t,e,n){const r=await R(t,e,n);_(n);const i=await window.crypto.subtle.importKey("raw",r,{name:"HMAC",hash:{name:"SHA-512"}},!1,["sign"]);_(n);const o=await window.crypto.subtle.sign("HMAC",i,this.hkdfPurpose);return _(n),o}}("Password"),Gt=e("txtPublicPart"),Ft=e("spnPublicPartSize"),Dt=e("btnGeneratePublicPart"),jt=e("btnClearPublicPart"),zt=e("btnCopyPublicPart"),Yt=e("btnShowHidePasswordOptionalFeatures"),Wt=e("lblAlphabetLength"),qt=e("numOutputSizeRange"),Jt=e("numOutputSizeNum"),Zt=e("lblAlphabet"),Xt=e("txtAlphabet"),Qt=e("spnAlphabetSize"),te=e("divPasswordAlphabetActions"),ee=e("btnResetAlphabet"),ne=e("txtResultPassword"),re=e("spnResultPasswordLength"),ie=e("btnViewResultPassword"),oe=e("btnCopyResultPassword"),ae=e("lblGeneratingPassword"),se=64,le="!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~";let ue,ce;function he(){return!(Gt.value.length>0&&"y"!==prompt("Are you sure you want to clear the public part ?\nType 'y' to accept","")||(r(Gt,!0),ve(),pe(),me(),0))}function de(){if(Gt.value.length>0&&"y"!==prompt("Are you sure you want to generate a new public part ?\nType 'y' to accept",""))return!1;const t=function(t=64,e=V){const n=function(t=64){const e=new Uint8Array(t);return crypto.getRandomValues(e).buffer}(t);return A(n,e)}();return Gt.value=t,ve(),pe(),_e(),!0}function pe(){ue=Gt.value.length>0?(new Date).toISOString():void 0}function fe(){re.innerHTML=ne.value.length.toString()}function ge(t){const e=t.split("");e.sort();for(let t=1;t<e.length;t+=1)if(e[t-1]===e[t])return!1;return!0}function me(){!1!==Le()?Nt({public:Gt.value,datetime:ue,version:Bt.version,length:ne.value.length,alphabet:Xt.value},"password"):Ae()}function we(){Jt.value=qt.value}async function ye(){we(),await _e()}async function be(){(function(){const t=parseInt(qt.min,10),e=parseInt(Jt.value,10),n=parseInt(qt.max,10);return!1===isNaN(e)&&(qt.value=Math.max(t,Math.min(e,n)).toString(),!0)})()&&we(),await _e()}function ve(){Ft.innerHTML=Gt.value.length.toString()}function Ee(){Qt.innerHTML=Xt.value.length.toString()}function Te(e){e?Xt.style.removeProperty("background"):Xt.style.setProperty("background",t)}async function Ce(){const t=ge(Xt.value);Te(t),!1!==t&&(Ee(),await _e())}async function Se(){return!1!==Re()&&(await _e(),!0)}function Ae(){r(ne),kt(),fe()}function Le(t){const e=Xt.value;return!1!==ge(e)&&(t=t||Gt.value,!(y().length<=0||t.length<8||e.length<2))}async function Pe(t,e,n){if(!1===Le(t))return null;const r=N(y()),i=N(t);return A(await Bt.generatePassword(r,i,n),e)}const Oe=new class{currentTokenSource=null;currentTask=null;microThreadId=0;get isRunning(){return null!==this.currentTask}async cancelInternal(t){this.microThreadId===Number.MAX_SAFE_INTEGER?this.microThreadId=0:this.microThreadId=this.microThreadId+1;const e=this.microThreadId;if(null===this.currentTask)return!0;if(null!==this.currentTokenSource&&(this.currentTokenSource.cancel(),null!==this.currentTask))try{await this.currentTask}catch(e){if(!L.isMatching(e))throw e;if(t)throw e}return e===this.microThreadId}async cancel(t=!1){await this.cancelInternal(t)}async cancelAndExecute(t,e=!1){if(!1===await this.cancelInternal(e)){if(!1===e)return;throw new L}var n=new P;this.currentTokenSource=n;try{return this.currentTask=t(this.currentTokenSource.token),await this.currentTask}catch(t){if(L.isMatching(t)&&!1===e)return;throw t}finally{this.currentTask=null}}};async function _e(){if(!1!==Le()){a(ae,!0);try{await Oe.cancelAndExecute(Ve)}finally{a(ae,!1)}}else Ae()}async function Ve(t){const e=await Pe(Gt.value,Xt.value,t);null!==e&&(ne.value=k(e,Math.max(4,parseInt(qt.value,10))),fe(),me(),ce())}function Re(){Xt.value=le,Ee();const t=ge(Xt.value);return Te(t),t}async function ke(){ve(),pe(),await _e()}const Ne=new Uint8Array([242,207,239,142,19,64,70,73,146,42,222,92,188,136,56,168]).buffer,Ie=[new class{get version(){return 1}get description(){return"PBKDF2 + AES-GCM"}async encrypt(t,e,n){const r=new ArrayBuffer(28+t.byteLength),i=new DataView(r,0,12);crypto.getRandomValues(new Uint8Array(r,0,12));const o={name:"AES-GCM",iv:i},a=await window.crypto.subtle.importKey("raw",await R(e,Ne,n),{name:"AES-GCM",length:256},!1,["encrypt"]);_(n);const s=await window.crypto.subtle.encrypt(o,a,t);return _(n),new Uint8Array(r).set(new Uint8Array(s),12),r}async decrypt(t,e,n){const r=new DataView(t,0,12),i=new DataView(t,12),o={name:"AES-GCM",iv:r},a=await R(e,Ne,n);_(n);const s=await window.crypto.subtle.importKey("raw",a,{name:"AES-GCM",length:256},!1,["decrypt"]);_(n);const l=await window.crypto.subtle.decrypt(o,s,i);return _(n),l}},new G],xe=e("btnTabReEncrypt"),He=e("divTabReEncrypt"),Me=e("txtReEncryptSource"),Ue=e("txtReEncryptTarget"),$e=e("cboReEncryptFrom"),Ke=e("cboReEncryptTo"),Be=e("btnReEncrypt"),Ge=e("btnClearReEncryptSource"),Fe=e("btnCopyReEncryptTarget"),De=e("btnClearReEncryptTarget");function je(t,e){let n;for(n of Ie){const e=document.createElement("option");e.value=t.childNodes.length.toString(),e.text=`${n.description} (v${n.version})`,t.add(e)}t.value=e.toString()}function ze(){Me.style.removeProperty("background-color")}async function Ye(){if(r(Ue,!0),ze(),Ue.style.removeProperty("background-color"),0===Me.value.length)return Me.style.setProperty("background-color",t),!1;if($e.value===Ke.value)return Ue.style.setProperty("background-color",t),!1;const e=y();if(0===e.length)return console.warn("Private part is empty"),!1;const n=parseInt($e.value,10),i=parseInt(Ke.value,10),o=N(e),a=function(t){t.length%2!=0&&(t="0"+t);const e=new Uint8Array(t.length/2);for(let n=0;n<e.byteLength;n+=1){const r=2*n;e[n]=parseInt(t.substring(r,r+2),16)}return e.buffer}(Me.value),s=await Ie[n].decrypt(a,o,O.none),l=await Ie[i].encrypt(s,o,O.none);var u;return Ue.value=(u=l,Array.prototype.map.call(new Uint8Array(u),(t=>("00"+t.toString(16)).slice(-2))).join("")),!0}const We=Math.floor(15);class qe{parent;children=[];rootElement;titleElement;childrenContainerElement;treeNodeCreationController;path;key;value;get element(){return this.rootElement}get isVisible(){return"none"!==this.rootElement.style.display}getVisibleChildCount(){let t=0;for(const e of this.children)e.isVisible&&(t+=1);return t}getVisibleLeafCount(){if(!1===this.isVisible)return 0;let t=1;for(const e of this.children)t+=e.getVisibleLeafCount();return t}addChild(t){this.childrenContainerElement.appendChild(t.rootElement),this.children.push(t)}createChildNodes(t){for(const[e,n]of Object.entries(t)){const t=new qe(this,`${this.path}/${e}`,e,n,this.treeNodeCreationController);this.addChild(t)}}constructor(t,e,n,r,i){this.parent=t,this.path=e,this.key=n,this.value=r,this.treeNodeCreationController=i,this.rootElement=document.createElement("div"),this.setRootElementStyle(),this.titleElement=document.createElement("div"),this.setTitleElementStyle(),this.titleElement.appendChild(this.createTreeNodeContentElement()),this.rootElement.appendChild(this.titleElement),this.childrenContainerElement=document.createElement("div"),this.rootElement.appendChild(this.childrenContainerElement),this.setChildrenContainerElementStyle();const o=i.isLeaf(e,n,r);!1===o&&I(r)?this.createChildNodes(r):o&&r.customKeys&&this.createChildNodes(r.customKeys),t&&this.setupLinesElements("#D0D0D0")}createTreeNodeContentElement(){return this.treeNodeCreationController.createTreeNodeContentElement(this.path,this.key,this.value)}setRootElementStyle(){this.rootElement.classList.add("treenode-root"),this.rootElement.style.display="grid";let t=4,e=0;this.parent&&(t=30),this.parent&&this.parent.parent&&(e=12),this.rootElement.style.gridTemplateRows=`${t}px 1fr`,this.rootElement.style.gridTemplateColumns=`${e}px 6px 1fr`}verticalLineElement=null;setupLinesElements(t){const e=document.createElement("div");if(e.classList.add("treenode-vertical-line"),e.style.gridColumn="2",e.style.gridRow="2",e.style.width="100%",e.style.borderRight=`1px solid ${t}`,this.verticalLineElement=e,this.rootElement.appendChild(e),this.parent&&this.parent.parent){const e=document.createElement("div");e.classList.add("treenode-horizontal-line"),e.style.gridColumn="1",e.style.gridRow="1",e.style.width="100%",e.style.height=`${We}px`,e.style.borderBottom=`1px solid ${t}`,this.rootElement.appendChild(e)}this.updateLines()}updateLines(){if(null===this.verticalLineElement)return;const t=this.getVisibleChildCount();if(0===t)return void(this.verticalLineElement.style.height="0px");let e=1;for(let n=0;n<t-1;n+=1)this.children[n].isVisible&&(e+=this.children[n].getVisibleLeafCount());const n=30*e-30+We+1;this.verticalLineElement.style.height=`${n}px`}setTitleElementStyle(){this.titleElement&&(this.titleElement.classList.add("treenode-title"),this.titleElement.style.gridColumn="2 / span 2",this.titleElement.style.gridRow="1",this.titleElement.style.marginLeft="3px",this.titleElement.style.alignSelf="center")}setChildrenContainerElementStyle(){this.childrenContainerElement.classList.add("treenode-children-container"),this.childrenContainerElement.style.gridColumn="3",this.childrenContainerElement.style.gridRow="2"}resetTitle(t){if(this.titleElement&&(this.titleElement.innerHTML="",this.titleElement.appendChild(this.createTreeNodeContentElement())),1===t&&this.parent&&this.parent.resetTitle(t),2===t)for(const e of this.children)e.resetTitle(t)}show(t){if(this.rootElement.style.display="grid",1===t&&this.parent&&this.parent.show(t),2===t)for(const e of this.children)e.show(t);this.updateLines()}hide(t){if(this.rootElement.style.display="none",1===t&&this.parent&&this.parent.hide(t),2===t)for(const e of this.children)e.hide(t);this.updateLines()}static createSpan(t,e){const n=document.createElement("span");return e&&(n.style.backgroundColor=e,n.style.borderRadius="2px"),n.innerText=t,n}static createColoredSpan(t,e){const n=document.createElement("span");let r=0;for(const i of e)i.pos!==r&&n.appendChild(qe.createSpan(t.substring(r,i.pos))),n.appendChild(qe.createSpan(t.substring(i.pos,i.pos+i.len),"#80C0FF")),r=i.pos+i.len;return r<t.length&&n.appendChild(qe.createSpan(t.substring(r,t.length))),n}static findLeafElement(t){return 0===t.children.length?t:qe.findLeafElement(t.children[0])}filter(t,e){if(!t)return this.resetTitle(2),this.show(2),void this.updateLines();const n=[];if(e(this.titleElement.innerText,t,n)){if(this.titleElement){const t=qe.findLeafElement(this.titleElement);t.innerHTML="";const e=this.createTreeNodeContentElement();t.appendChild(qe.createColoredSpan(e.innerText,n))}this.show(1),this.show(2)}else this.resetTitle(2);for(const n of this.children)n.filter(t,e);this.updateLines()}}function Je(t,e,n,r){if(!n)return!0;t=t.toLowerCase();for(let i=(n=n.toLowerCase()).length;i>=1;i-=1){const o=n.substring(0,i),a=t.indexOf(o,e);if(a>=0)return r.push({pos:a,len:o.length}),Je(t,a+o.length,n.substring(i),r)}return!1}const Ze=e("btnTabVaultTabTreeView"),Xe=e("divTabVaultTabTreeView"),Qe=e("trvVaultTreeView"),tn=e("txtVaultTreeViewSearch"),en=e("cboVaultTreeViewSearchType");let nn;const rn=[{text:"Aggresive",function:function(t,e,n){return Je(t,0,e,n)}},{text:"Regular",function:function(t,e,n){const r=t.toLowerCase().indexOf(e.toLowerCase());return!(r<0||(n.push({pos:r,len:e.length}),0))}}];function on(){if(!nn)return;const t=en.selectedIndex,e=rn[t].function;nn.hide(2),nn.filter(tn.value.toLocaleLowerCase(),e)}class an{passwordService;constructor(){this.passwordService=M("password")}async runPassword(t){await this.passwordService.generateAndCopyPasswordToClipboard(t.public,t.alphabet,t.length)}async runCipher(t,e,n){const r=M("cipher");return null!==r&&await r.activate(t,e,n)}static isPasswordObject(t,e){return"password"===t&&!(!e||!I(e)||"string"!=typeof e.public||e.public.length<4)}static isCipherObject(t){return!(!t||!I(t)||"string"!=typeof t.value||t.value.length<=0||"number"!=typeof t.version||t.version<0)}static isCiphersObject(t,e){if("ciphers"!==t)return!1;if(!e||!I(e))return!1;for(const t of Object.values(e))if(!an.isCipherObject(t))return!1;return!0}static isHint(t,e){return!(an.isCiphersObject(t,e)||an.isCipherObject(e)||an.isPasswordObject(t,e)||I(e))}isLeaf(t,e,n){return!(!an.isCipherObject(n)&&!an.isPasswordObject(e,n))||!1===I(n)}createTreeNodeContentElement(t,e,n){if(an.isPasswordObject(e,n)){const t=document.createElement("button");return t.style.justifySelf="start",t.style.minWidth="80px",t.innerText="Password",i(t,(async()=>await this.runPassword(n))),t}if(an.isCipherObject(n)){const r=document.createElement("button");return r.style.justifySelf="start",r.innerText=e,r.title="Open in ciphers",i(r,(async()=>await this.runCipher(t,e,n))),r}if(an.isHint(e,n)){const t=document.createElement("span");return t.style.justifySelf="start",t.innerText=`${e}: ${n}`,t}const r=document.createElement("div");return r.innerText=e,r}}const sn=e("btnTabVaultTabTextView"),ln=e("divTabVaultTabTextView"),un=e("txtVault");class cn{vaultComponent;constructor(t){this.vaultComponent=t}computeUserPathMatchDepth(t){return this.vaultComponent.computeUserPathMatchDepth(t)}}const hn=e("divTabVault"),dn=e("btnTabVault"),pn=e("btnRefreshVault"),fn=e("btnClearVaultSettings"),gn=[new class{name="VaultTreeView";onVaultLoaded(t){nn=new qe(null,"<root>","",t,new an),Qe.innerHTML="",Qe.appendChild(nn.element),on()}getTabButton(){return Ze}getTabContent(){return Xe}onTabSelected(){tn.focus()}getVaultHint(){throw new Error("Not supported.")}init(){!function(){en.innerHTML="";for(let t of rn){const e=document.createElement("option");e.text=t.text,en.appendChild(e)}}(),tn.addEventListener("input",on),en.addEventListener("change",on)}},new class{name="VaultTextView";onVaultLoaded(t){un.value=JSON.stringify(t,void 0,4)}getTabButton(){return sn}getTabContent(){return ln}onTabSelected(){}getVaultHint(){throw new Error("Not supported.")}init(){}}],mn=gn.filter((t=>void 0!==t.getTabButton)),wn=gn.filter((t=>void 0!==t.init)),yn=new l(mn);let bn=new K(new gt),vn=null;async function En(){if(y().length>0==0)return alert("You must enter a master key first."),!1;const t=await async function(){let t=await bn.getVaultContent();if(null===t)return!1;try{let e,n=JSON.parse(t);for(e of(n=x(n),vn=n,wn))e.onVaultLoaded(n);return!0}catch(t){return vn=null,alert("Failed to parse vault content, needs to be fixed."),console.error(t.message),!1}}();return t&&b(),t}function Tn(){"y"===prompt("Are you sure you want to clear the vault settings ?\nType 'y' to accept","")&&bn.clear()}const Cn=[{getTabButton:()=>e("btnTabNothing"),getTabContent:()=>e("divTabNothing"),onTabSelected(){Mt()}},new class{name="PrivatePart";getVaultHint(){throw new Error("Not supported.")}init(){d.addEventListener("click",v),c.addEventListener("input",T),h.addEventListener("input",S),C(),d.title=u,c.focus()}},new class{name="Password";getTabButton(){return $t}getTabContent(){return Kt}onTabSelected(){Ht(),me(),Gt.focus()}getVaultHint(){return this.name.toLowerCase()}init(){var t,e,n;t=_e,w.push(t),qt.max=se.toString(),qt.value=se.toString(),i(jt,he),i(Dt,de),e=ne,(n=ie).addEventListener("click",(()=>{"password"===e.type?(e.type="input",n.innerHTML="Hide"):(e.type="password",n.innerHTML="View")})),o(Gt,zt),ce=o(ne,oe),qt.addEventListener("input",ye),Jt.addEventListener("input",be),Xt.addEventListener("input",Ce),i(ee,Se),Gt.addEventListener("input",ke),a(ae,!1),function(t,e,n){let r=!1;t.addEventListener("click",(function(){r=!r,s(n,r)})),s(n,r)}(Yt,0,[Zt,Xt,Qt,te,Wt,qt,Jt]),ve(),we(),Re(),U("password",new Ut)}},new ft,new class{name="ReEncrypt";getTabButton(){return xe}getTabContent(){return He}onTabSelected(){Mt(),Me.focus()}getVaultHint(){throw new Error("Not supported.")}init(){o(Ue,Fe),je($e,Ie.length-2),je(Ke,Ie.length-1),Me.addEventListener("input",(()=>{Me.value.length>0&&ze()})),Ge.addEventListener("click",(()=>{r(Me,!0)})),De.addEventListener("click",(()=>{r(Ue,!0)})),i(Be,Ye)}},new class{name="StorageOutput";getVaultHint(){throw new Error("Not supported.")}init(){Et.addEventListener("input",Pt),i(vt,Rt),wt.addEventListener("input",Lt)}},new class{name="Vault";computeUserPathMatchDepth(t){return function(t){if(null===vn)return 0;let e=vn;const n=t.split("/");for(let t=0;t<n.length;t+=1){if(!e[n[t]])return t;e=e[n[t]]}return n.length}(t)}getTabButton(){return dn}getTabContent(){return hn}onTabSelected(){Mt(),mn[yn.activeTabIndex].onTabSelected()}getVaultHint(){throw new Error("Not supported.")}init(){i(pn,En),fn.addEventListener("click",Tn);const t=new cn(this);let e;for(e of(U("vault",t),wn))e.init()}}],Sn=Cn.filter((t=>void 0!==t.getTabButton)),An=Cn.filter((t=>void 0!==t.init)),Ln=new l(Sn),Pn=new class{name="Root";constructor(){}getVaultHint(){throw new Error("Not supported.")}init(){let t;for(t of An)t.init()}getActiveComponent(){const t=Sn[Ln.activeTabIndex];return void 0!==t.init?t:null}},On="117bb08bb2ccb3a66808b6894daa0a31410a4e2b".substring(0,11);e("divInfo").innerHTML=`${On}<br/><a href="https://github.com/TanukiSharp/ItchyPassword" target="_blank">github</a>`,Pn.init()})();
//# sourceMappingURL=bundle.js.map