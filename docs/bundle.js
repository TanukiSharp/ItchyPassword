!function(t){var e={};function n(r){if(e[r])return e[r].exports;var a=e[r]={i:r,l:!1,exports:{}};return t[r].call(a.exports,a,a.exports,n),a.l=!0,a.exports}n.m=t,n.c=e,n.d=function(t,e,r){n.o(t,e)||Object.defineProperty(t,e,{enumerable:!0,get:r})},n.r=function(t){"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(t,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(t,"__esModule",{value:!0})},n.t=function(t,e){if(1&e&&(t=n(t)),8&e)return t;if(4&e&&"object"==typeof t&&t&&t.__esModule)return t;var r=Object.create(null);if(n.r(r),Object.defineProperty(r,"default",{enumerable:!0,value:t}),2&e&&"string"!=typeof t)for(var a in t)n.d(r,a,function(e){return t[e]}.bind(null,a));return r},n.n=function(t){var e=t&&t.__esModule?function(){return t.default}:function(){return t};return n.d(e,"a",e),e},n.o=function(t,e){return Object.prototype.hasOwnProperty.call(t,e)},n.p="",n(n.s=0)}([function(t,e,n){"use strict";n.r(e);class r{constructor(t){this.element=t}setText(t,e){this.element.innerHTML=t,this.timeout&&clearTimeout(this.timeout),this.timeout=setTimeout(()=>this.element.innerHTML="",e)}}function a(t){const e=document.getElementById(t);if(null===t)throw new Error(`DOM element '${t}' not found.`);return e}function o(t,e,n){const a=new r(n);e.addEventListener("click",async()=>{await async function(t){try{return await navigator.clipboard.writeText(t),!0}catch(t){return console.error(t.stack||t),!1}}(t.value)?a.setText("Copied",3e3):a.setText('<span style="color: red">Failed to copy</span>',3e3)})}const i="#D0FFD0",s="#FFD0D0";const c="Stores the string in memory and removes it from the UI component. Prevents a physical intruder from copy/pasting the value.",u="Removes the string form memory and re-enables the UI component.",l=a("txtPrivatePart"),y=a("txtPrivatePartConfirmation"),d=a("btnProtect"),p=a("spnProtectedConfirmation"),v=a("spnPrivatePartSize"),b=a("spnPrivatePartSizeConfirmation");let f,g=[];function h(){return void 0!==f?f:l.value}function w(){0!==l.value.length&&(f=l.value,p.innerHTML="Protected",l.value="",y.value="",v.innerHTML="0",b.innerHTML="0",l.disabled=!0,y.disabled=!0,d.innerHTML="Clear and unlock",d.title=u,T())}function m(){void 0===f?w():(f=void 0,p.innerHTML="",l.disabled=!1,y.disabled=!1,d.innerHTML="Protect and lock",d.title=c,d.disabled=!0)}d.addEventListener("click",()=>{m()});const P=new class{constructor(t,e){this.action=t,this.delay=e}reset(t){void 0!==this.timeout&&clearTimeout(this.timeout);const e=void 0!==t?t:this.delay;this.timeout=setTimeout(()=>{this.action(),this.timeout=void 0},e)}}(w,6e4);function T(){y.value===l.value?y.style.setProperty("background",i):y.style.setProperty("background",s)}function E(t){const e=(t=function(t){if(t.byteLength>65535)throw new Error(`Buffer too large: ${t.byteLength} bytes`);let e=t.byteLength;const n=new Uint8Array(2+t.byteLength);for(let t=0;t<2;t+=1)n[t]=e%256,e/=256;return n.set(new Uint8Array(t),2),n.buffer}(t)).byteLength,n=new DataView(t,0);let r=0n,a=1n;for(let t=0;t<e;t+=1)r+=BigInt(n.getUint8(t))*a,a*=256n;return r}function L(t,e){const n=BigInt(e.length);let r="",a=function(t){const e=t.byteLength,n=new DataView(t,0);let r=0n,a=1n;for(let t=0;t<e;t+=1)r+=BigInt(n.getUint8(t))*a,a*=256n;return r}(t);for(;a>0n;){const t=a%n;a/=n,r+=e[BigInt.asUintN(8,t)]}return r}function A(t,e){const n=BigInt(e.length);let r=0n,a=1n;for(let o=0;o<t.length;o+=1){r+=BigInt(e.indexOf(t[o]))*a,a*=n}return function(t){const e=[];for(;t>0n;){const n=t%256n;t/=256n;const r=Number(BigInt.asUintN(8,n));e.push(r)}let n=e[0];e.length>1&&(n+=256*e[1]);const r=n-(e.length-2);for(let t=0;t<r;t+=1)e.push(0);return new Uint8Array(e.slice(2)).buffer}(r)}l.addEventListener("input",()=>{let t;for(t of(d.disabled=0===l.value.length,v.innerHTML=l.value.length.toString(),T(),g))t();P.reset()}),y.addEventListener("input",()=>{b.innerHTML=y.value.length.toString(),P.reset(),T()}),T(),d.title=c;const S="abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";async function C(t,e){const n=await crypto.subtle.importKey("raw",t,"PBKDF2",!1,["deriveKey"]),r={name:"PBKDF2",hash:"SHA-512",iterations:1e5,salt:e},a=await crypto.subtle.deriveKey(r,n,{name:"AES-CBC",length:256},!0,["encrypt"]);return await crypto.subtle.exportKey("raw",a)}function k(t=64,e=S){return L(function(t=64){const e=new Uint8Array(t);return crypto.getRandomValues(e).buffer}(t),e)}function M(t){return(new TextEncoder).encode(t).buffer}var x;x=ct,g.push(x);const I=new class{constructor(t){this.hkdfPurpose=M(t),this._description=`HKDF(PBKDF2, HMAC512) [purpose: ${t}]`}get version(){return 1}get description(){return this._description}async generatePassword(t,e){const n=await C(t,e),r=await crypto.subtle.importKey("raw",n,{name:"HMAC",hash:{name:"SHA-512"}},!1,["sign"]);return await crypto.subtle.sign("HMAC",r,this.hkdfPurpose)}}("Password"),H=a("txtPath"),U=a("txtPublicPart"),R=a("btnGeneratePublicPart"),O=a("btnClearPublicPart"),B=a("btnCopyPublicPart"),D=a("spnCopyPublicPartFeedback"),F=a("numOutputSizeRange"),K=a("numOutputSizeNum"),j=a("txtAlphabet"),N=a("spnAlphabetSize"),G=a("btnResetAlphabet"),V=a("txtResultPassword"),_=a("spnResultPasswordLength"),$=a("btnCopyResultPassword"),z=a("spnCopyResultPasswordFeedback"),J=a("txtParameters"),q=a("txtCustomKeys"),Q=64,W="!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~",X=["alphabet","length","public","datetime"];let Y;function Z(){Y=U.value.length>0?(new Date).toISOString():void 0}function tt(){_.innerHTML=V.value.length.toString().padStart(2," ")}function et(t){const e=t.split("");e.sort();for(let t=1;t<e.length;t+=1)if(e[t-1]===e[t])return!1;return!0}function nt(){if(!1===st())return void it();const t=function t(e,n){const r=e.indexOf("/"),a={},o=r>=0?e.substr(0,r):e,i=r>=0?e.substr(r+1):void 0;if(void 0===n){const t={};t[o]=a,n={head:t,tailParent:t,tail:a}}else n.tail[o]=a,n.tailParent=n.tail,n.tail=a;return i?t(i,n):n}(H.value),e=t.tail;e.public=U.value,e.datetime=Y;const n=V.value.length;n!==Q&&(e.length=n);const r=j.value;r!==W&&(e.alphabet=r);const a=function(){if(""===q.value)return{};try{const t=JSON.parse(q.value);return null===t||"Object"!==t.constructor.name?null:t}catch{return null}}();null!==a?q.style.removeProperty("background"):q.style.setProperty("background",s);const o=function(t,e){const n={};if(null!==t)for(const[e,r]of Object.entries(t))!1===X.includes(e)&&(n[e]=r);if(null!==e)for(const[t,r]of Object.entries(e))n[t]=r;return n}(a,e);0===Object.keys(o).length?t.tailParent[Object.keys(t.tailParent)[0]]=null:t.tailParent[Object.keys(t.tailParent)[0]]=o,J.value=JSON.stringify(t.head,void 0,4)}function rt(){K.value=F.value}function at(){N.innerHTML=j.value.length.toString(),j.value.length.toString().length<2&&(N.innerHTML=N.innerHTML.padStart(2," "))}function ot(t){t?j.style.removeProperty("background"):j.style.setProperty("background",s)}function it(){V.value="",J.value="",tt()}function st(){const t=j.value;return!1!==et(t)&&!(h().length<=0||U.value.length<8||t.length<2)}async function ct(){if(!1===st())return void it();const t=h(),e=U.value,n=M(t),r=M(e),a=L(await I.generatePassword(n,r),j.value);var o,i;V.value=(o=a,i=Math.max(4,parseInt(F.value,10)),o.length<=i?o:o.substr(0,i)),tt(),nt()}async function ut(){j.value=W,at();const t=et(j.value);ot(t),t&&await ct()}F.max=Q.toString(),F.value=Q.toString(),O.addEventListener("click",()=>{U.value.length>0&&"y"!==prompt("Are you sure you want to clear the public part ?\nType 'y' to accept","")||(U.value="",Z(),nt())}),R.addEventListener("click",()=>{if(U.value.length>0&&"y"!==prompt("Are you sure you want to generate a new public part ?\nType 'y' to accept",""))return;const t=k();U.value=t,Z(),ct()}),function(t,e){const n=a(e);n.addEventListener("click",()=>{"password"===t.type?(t.type="input",n.innerHTML="Hide"):(t.type="password",n.innerHTML="View")})}(V,"btnViewResultPassword"),o(U,B,D),o(V,$,z),F.addEventListener("input",async()=>{rt(),await ct()}),K.addEventListener("input",async()=>{(function(){const t=parseInt(F.min,10),e=parseInt(K.value,10),n=parseInt(F.max,10);return!1===isNaN(e)&&(F.value=Math.max(t,Math.min(e,n)).toString(),!0)})()&&rt(),await ct()}),j.addEventListener("input",async()=>{const t=et(j.value);ot(t),!1!==t&&(at(),await ct())}),G.addEventListener("click",async()=>{ut(),at(),await ct()}),H.addEventListener("input",()=>{nt()}),U.addEventListener("input",async()=>{Z(),await ct()}),q.addEventListener("input",()=>{nt()}),rt(),ut();class lt{get version(){return 2}get description(){return"PBKDF2 + AES-GCM"}async encrypt(t,e){const n=new ArrayBuffer(44+t.byteLength),r=crypto.getRandomValues(new Uint8Array(n,0,12)),a=crypto.getRandomValues(new Uint8Array(n,12,16)),o={name:"AES-GCM",iv:r},i=await crypto.subtle.importKey("raw",await C(e,a),{name:"AES-GCM",length:256},!1,["encrypt"]),s=await crypto.subtle.encrypt(o,i,t);return new Uint8Array(n).set(new Uint8Array(s),28),n}async decrypt(t,e){const n=new Uint8Array(t,0,12),r=new Uint8Array(t,12,16),a=new Uint8Array(t,28),o={name:"AES-GCM",iv:n},i=await C(e,r),s=await crypto.subtle.importKey("raw",i,{name:"AES-GCM",length:256},!1,["decrypt"]);return await crypto.subtle.decrypt(o,s,a)}}const yt=new lt,dt=a("txtCipherSource"),pt=a("txtCipherTarget"),vt=a("btnEncrypt"),bt=a("btnDecrypt"),ft=a("btnClearCipherSource"),gt=a("spnCopyCipherTargetFeedback"),ht=a("btnCopyCipherTarget"),wt=a("btnClearCipherTarget");function mt(){dt.style.removeProperty("background-color")}function Pt(){dt.style.setProperty("background-color",s)}function Tt(){mt(),pt.style.removeProperty("background-color")}o(pt,ht,gt),vt.addEventListener("click",async()=>{if(dt.focus(),pt.value="",Tt(),0===dt.value.length)return void Pt();const t=h();if(0===t.length)return void console.warn("Private part is empty");const e=M(dt.value),n=M(t),r=await yt.encrypt(e,n);pt.value=function(t,e){const n=BigInt(e.length);let r="",a=E(t);for(;a>0n;){const t=a%n;a/=n,r+=e[BigInt.asUintN(8,t)]}return r}(r,S)}),bt.addEventListener("click",async()=>{if(dt.focus(),pt.value="",Tt(),0===dt.value.length)return void Pt();const t=h();if(0!==t.length)try{const e=A(dt.value,S),n=M(t),r=await yt.decrypt(e,n);pt.value=function(t){return(new TextDecoder).decode(t)}(r)}catch(t){console.warn(`Failed to decrypt${t.message?`, error: ${t.message}`:", no error message"}`),pt.style.setProperty("background-color",s)}else console.warn("Private part is empty")}),dt.addEventListener("input",()=>{dt.value.length>0&&mt()}),ft.addEventListener("click",()=>{dt.value=""}),wt.addEventListener("click",()=>{pt.value=""});const Et=new Uint8Array([242,207,239,142,19,64,70,73,146,42,222,92,188,136,56,168]).buffer;const Lt=[new class{get version(){return 1}get description(){return"PBKDF2 + AES-GCM"}async encrypt(t,e){const n=new ArrayBuffer(28+t.byteLength),r=new DataView(n,0,12);crypto.getRandomValues(new Uint8Array(n,0,12));const a={name:"AES-GCM",iv:r},o=await crypto.subtle.importKey("raw",await C(e,Et),{name:"AES-GCM",length:256},!1,["encrypt"]),i=await crypto.subtle.encrypt(a,o,t);return new Uint8Array(n).set(new Uint8Array(i),12),n}async decrypt(t,e){const n=new DataView(t,0,12),r=new DataView(t,12),a={name:"AES-GCM",iv:n},o=await C(e,Et),i=await crypto.subtle.importKey("raw",o,{name:"AES-GCM",length:256},!1,["decrypt"]);return await crypto.subtle.decrypt(a,i,r)}},new lt],At=a("txtReEncryptSource"),St=a("txtReEncryptTarget"),Ct=a("cboReEncryptFrom"),kt=a("cboReEncryptTo"),Mt=a("btnReEncrypt"),xt=a("btnClearReEncryptSource"),It=a("spnCopyReEncryptTargetFeedback"),Ht=a("btnCopyReEncryptTarget"),Ut=a("btnClearReEncryptTarget");function Rt(t,e){let n;for(n of Lt){const e=document.createElement("option");e.value=t.childNodes.length.toString(),e.text=`${n.description} (v${n.version})`,t.add(e)}t.value=e.toString()}function Ot(){At.style.removeProperty("background-color")}function Bt(){Ot(),St.style.removeProperty("background-color")}o(St,Ht,It),Rt(Ct,Lt.length-2),Rt(kt,Lt.length-1),Mt.addEventListener("click",async()=>{if(St.value="",Bt(),0===At.value.length)return void At.style.setProperty("background-color",s);if(Ct.value===kt.value)return void St.style.setProperty("background-color",s);const t=h();if(0===t.length)return void console.warn("Private part is empty");const e=parseInt(Ct.value,10),n=parseInt(kt.value,10),r=M(t),a=function(t){t.length%2!=0&&(t="0"+t);const e=new Uint8Array(t.length/2);for(let n=0;n<e.byteLength;n+=1)e[n]=parseInt(t.substr(2*n,2),16);return e.buffer}(At.value),o=await Lt[e].decrypt(a,r),i=await Lt[n].encrypt(o,r);St.value=function(t){return Array.prototype.map.call(new Uint8Array(t),t=>("00"+t.toString(16)).slice(-2)).join("")}(i)}),At.addEventListener("input",()=>{At.value.length>0&&Ot()}),xt.addEventListener("click",()=>{At.value=""}),Ut.addEventListener("click",()=>{St.value=""});const Dt=a("btnTabNothing"),Ft=a("btnTabPasswords"),Kt=a("btnTabCiphers"),jt=a("btnTabReEncrypt");new class{constructor(t){this.tabs=t,this._activeTabIndex=-1;for(let e=0;e<this.tabs.length;e+=1)t[e].button.addEventListener("click",()=>{this.setActiveTab(e)});this.setActiveTab(0)}get activeTabIndex(){return this._activeTabIndex}set activeTabIndex(t){if(t<0||t>=this.tabs.length)throw new Error(`Argument 'index' out of range. Must be in range [0;${this.tabs.length-1}].`);this.setActiveTab(t)}setActiveTab(t){let e;for(e of this.tabs)e.button.style.removeProperty("font-weight"),e.button.style.setProperty("color","#C0C0C0"),e.content.style.setProperty("display","none");this.tabs[t].button.style.setProperty("font-weight","bold"),this.tabs[t].button.style.removeProperty("color"),this.tabs[t].content.style.removeProperty("display"),this._activeTabIndex=t}}([{button:Dt,content:a("divTabNothing")},{button:Ft,content:a("divTabPasswords")},{button:Kt,content:a("divTabCiphers")},{button:jt,content:a("divTabReEncrypt")}]);const Nt="8e34044b1373a27e5c3d542bd692b519b3895ba9".substr(0,11);a("divInfo").innerHTML=`${Nt}<br/><a href="https://github.com/TanukiSharp/ItchyPassword" target="_blank">github</a>`}]);
//# sourceMappingURL=bundle.js.map