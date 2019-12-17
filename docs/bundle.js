!function(t){var e={};function n(r){if(e[r])return e[r].exports;var a=e[r]={i:r,l:!1,exports:{}};return t[r].call(a.exports,a,a.exports,n),a.l=!0,a.exports}n.m=t,n.c=e,n.d=function(t,e,r){n.o(t,e)||Object.defineProperty(t,e,{enumerable:!0,get:r})},n.r=function(t){"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(t,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(t,"__esModule",{value:!0})},n.t=function(t,e){if(1&e&&(t=n(t)),8&e)return t;if(4&e&&"object"==typeof t&&t&&t.__esModule)return t;var r=Object.create(null);if(n.r(r),Object.defineProperty(r,"default",{enumerable:!0,value:t}),2&e&&"string"!=typeof t)for(var a in t)n.d(r,a,function(e){return t[e]}.bind(null,a));return r},n.n=function(t){var e=t&&t.__esModule?function(){return t.default}:function(){return t};return n.d(e,"a",e),e},n.o=function(t,e){return Object.prototype.hasOwnProperty.call(t,e)},n.p="",n(n.s=0)}([function(t,e,n){"use strict";n.r(e);class r{constructor(t){this.element=t}setText(t,e){this.element.innerHTML=t,this.timeout&&clearTimeout(this.timeout),this.timeout=setTimeout(()=>this.element.innerHTML="",e)}}function a(t){const e=document.getElementById(t);if(null===t)throw new Error(`DOM element '${t}' not found.`);return e}function o(t,e,n){const a=new r(n);e.addEventListener("click",async()=>{await async function(t){try{return await navigator.clipboard.writeText(t),!0}catch(t){return console.error(t.stack||t),!1}}(t.value)?a.setText("Copied",3e3):a.setText('<span style="color: red">Failed to copy</span>',3e3)})}const i="#D0FFD0",s="#FFD0D0";const c="Stores the string in memory and removes it from the UI component. Prevents a physical intruder from copy/pasting the value.",u="Removes the string form memory and re-enables the UI component.",l=a("txtPrivatePart"),d=a("txtPrivatePartConfirmation"),p=a("btnProtect"),y=a("spnProtectedConfirmation"),v=a("spnPrivatePartSize"),b=a("spnPrivatePartSizeConfirmation");let f,h=[];function g(){return void 0!==f?f:l.value}function w(){0!==l.value.length&&(f=l.value,y.innerHTML="Protected",l.value="",d.value="",v.innerHTML="0",b.innerHTML="0",l.disabled=!0,d.disabled=!0,p.innerHTML="Clear and unlock",p.title=u,T())}function m(){void 0===f?w():(f=void 0,y.innerHTML="",l.disabled=!1,d.disabled=!1,p.innerHTML="Protect and lock",p.title=c,p.disabled=!0)}p.addEventListener("click",()=>{m()});const P=new class{constructor(t,e){this.action=t,this.delay=e}reset(t){void 0!==this.timeout&&clearTimeout(this.timeout);const e=void 0!==t?t:this.delay;this.timeout=setTimeout(()=>{this.action(),this.timeout=void 0},e)}}(w,6e4);function T(){d.value===l.value?d.style.setProperty("background",i):d.style.setProperty("background",s)}function E(t,e){const n=BigInt(e.length);let r="",a=function(t){const e=t.byteLength,n=new DataView(t,0);let r=0n;for(let t=0;t<e;t+=1)r+=BigInt(n.getUint8(t))*256n**BigInt(t);return r}(t);for(;a>0n;){const t=a%n;a/=n,r+=e[BigInt.asUintN(64,t)]}return r}function L(t){return Array.prototype.map.call(new Uint8Array(t),t=>("00"+t.toString(16)).slice(-2)).join("")}l.addEventListener("input",()=>{let t;for(t of(p.disabled=0===l.value.length,v.innerHTML=l.value.length.toString(),T(),h))t();P.reset()}),d.addEventListener("input",()=>{b.innerHTML=d.value.length.toString(),P.reset(),T()}),T(),p.title=c;const S="abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";async function C(t,e){const n=await crypto.subtle.importKey("raw",t,"PBKDF2",!1,["deriveKey"]),r={name:"PBKDF2",hash:"SHA-512",iterations:1e5,salt:e},a=await crypto.subtle.deriveKey(r,n,{name:"AES-CBC",length:256},!0,["encrypt"]);return await crypto.subtle.exportKey("raw",a)}function A(t=64,e=S){return E(function(t=64){const e=new Uint8Array(t);return crypto.getRandomValues(e).buffer}(t),e)}function k(t){return(new TextEncoder).encode(t).buffer}function M(t){t.length%2!=0&&(t="0"+t);const e=new Uint8Array(t.length/2);for(let n=0;n<e.byteLength;n+=1)e[n]=parseInt(t.substr(2*n,2),16);return e.buffer}var x;x=ct,h.push(x);const H=new class{constructor(t){this.hkdfPurpose=k(t),this._description=`HKDF(PBKDF2, HMAC512) [purpose: ${t}]`}get version(){return 1}get description(){return this._description}async generatePassword(t,e){const n=await C(t,e),r=await crypto.subtle.importKey("raw",n,{name:"HMAC",hash:{name:"SHA-512"}},!1,["sign"]);return await crypto.subtle.sign("HMAC",r,this.hkdfPurpose)}}("Password"),R=a("txtPath"),I=a("txtPublicPart"),O=a("btnGeneratePublicPart"),D=a("btnClearPublicPart"),U=a("btnCopyPublicPart"),F=a("spnCopyPublicPartFeedback"),K=a("numOutputSizeRange"),j=a("numOutputSizeNum"),B=a("txtAlphabet"),G=a("spnAlphabetSize"),_=a("btnResetAlphabet"),V=a("txtResultPassword"),N=a("spnResultPasswordLength"),$=a("btnCopyResultPassword"),z=a("spnCopyResultPasswordFeedback"),J=a("txtParameters"),q=a("txtCustomKeys"),Q=64,W="!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~",X=["alphabet","length","public","datetime"];let Y;function Z(){Y=I.value.length>0?(new Date).toISOString():void 0}function tt(){N.innerHTML=V.value.length.toString().padStart(2," ")}function et(t){const e=t.split("");e.sort();for(let t=1;t<e.length;t+=1)if(e[t-1]===e[t])return!1;return!0}function nt(){if(!1===st())return void it();const t=function t(e,n){const r=e.indexOf("/"),a={},o=r>=0?e.substr(0,r):e,i=r>=0?e.substr(r+1):void 0;if(void 0===n){const t={};t[o]=a,n={head:t,tailParent:t,tail:a}}else n.tail[o]=a,n.tailParent=n.tail,n.tail=a;return i?t(i,n):n}(R.value),e=t.tail;e.public=I.value,e.datetime=Y;const n=V.value.length;n!==Q&&(e.length=n);const r=B.value;r!==W&&(e.alphabet=r);const a=function(){if(""===q.value)return{};try{const t=JSON.parse(q.value);return null===t||"Object"!==t.constructor.name?null:t}catch{return null}}();null!==a?q.style.removeProperty("background"):q.style.setProperty("background",s);const o=function(t,e){const n={};if(null!==t)for(const[e,r]of Object.entries(t))!1===X.includes(e)&&(n[e]=r);if(null!==e)for(const[t,r]of Object.entries(e))n[t]=r;return n}(a,e);0===Object.keys(o).length?t.tailParent[Object.keys(t.tailParent)[0]]=null:t.tailParent[Object.keys(t.tailParent)[0]]=o,J.value=JSON.stringify(t.head,void 0,4)}function rt(){j.value=K.value}function at(){G.innerHTML=B.value.length.toString(),B.value.length.toString().length<2&&(G.innerHTML=G.innerHTML.padStart(2," "))}function ot(t){t?B.style.removeProperty("background"):B.style.setProperty("background",s)}function it(){V.value="",J.value="",tt()}function st(){const t=B.value;return!1!==et(t)&&!(g().length<=0||I.value.length<8||t.length<2)}async function ct(){if(!1===st())return void it();const t=g(),e=I.value,n=k(t),r=k(e),a=E(await H.generatePassword(n,r),B.value);var o,i;V.value=(o=a,i=Math.max(4,parseInt(K.value,10)),o.length<=i?o:o.substr(0,i)),tt(),nt()}async function ut(){B.value=W,at();const t=et(B.value);ot(t),t&&await ct()}K.max=Q.toString(),K.value=Q.toString(),D.addEventListener("click",()=>{I.value.length>0&&"y"!==prompt("Are you sure you want to clear the public part ?\nType 'y' to accept","")||(I.value="",Z(),nt())}),O.addEventListener("click",()=>{if(I.value.length>0&&"y"!==prompt("Are you sure you want to generate a new public part ?\nType 'y' to accept",""))return;const t=A();I.value=t,Z(),ct()}),function(t,e){const n=a(e);n.addEventListener("click",()=>{"password"===t.type?(t.type="input",n.innerHTML="Hide"):(t.type="password",n.innerHTML="View")})}(V,"btnViewResultPassword"),o(I,U,F),o(V,$,z),K.addEventListener("input",async()=>{rt(),await ct()}),j.addEventListener("input",async()=>{(function(){const t=parseInt(K.min,10),e=parseInt(j.value,10),n=parseInt(K.max,10);return!1===isNaN(e)&&(K.value=Math.max(t,Math.min(e,n)).toString(),!0)})()&&rt(),await ct()}),B.addEventListener("input",async()=>{const t=et(B.value);ot(t),!1!==t&&(at(),await ct())}),_.addEventListener("click",async()=>{ut(),at(),await ct()}),R.addEventListener("input",()=>{nt()}),I.addEventListener("input",async()=>{Z(),await ct()}),q.addEventListener("input",()=>{nt()}),rt(),ut();class lt{get version(){return 2}get description(){return"PBKDF2 + AES-GCM"}async encrypt(t,e){const n=new ArrayBuffer(44+t.byteLength),r=crypto.getRandomValues(new Uint8Array(n,0,12)),a=crypto.getRandomValues(new Uint8Array(n,12,16)),o={name:"AES-GCM",iv:r},i=await crypto.subtle.importKey("raw",await C(e,a),{name:"AES-GCM",length:256},!1,["encrypt"]),s=await crypto.subtle.encrypt(o,i,t);return new Uint8Array(n).set(new Uint8Array(s),28),n}async decrypt(t,e){const n=new Uint8Array(t,0,12),r=new Uint8Array(t,12,16),a=new Uint8Array(t,28),o={name:"AES-GCM",iv:n},i=await C(e,r),s=await crypto.subtle.importKey("raw",i,{name:"AES-GCM",length:256},!1,["decrypt"]);return await crypto.subtle.decrypt(o,s,a)}}const dt=new lt,pt=a("txtCipherSource"),yt=a("txtCipherTarget"),vt=a("btnEncrypt"),bt=a("btnDecrypt"),ft=a("btnClearCipherSource"),ht=a("spnCopyCipherTargetFeedback"),gt=a("btnCopyCipherTarget"),wt=a("btnClearCipherTarget");function mt(){pt.style.removeProperty("background-color")}function Pt(){pt.style.setProperty("background-color",s)}function Tt(){mt(),yt.style.removeProperty("background-color")}o(yt,gt,ht),vt.addEventListener("click",async()=>{if(pt.focus(),yt.value="",Tt(),0===pt.value.length)return void Pt();const t=g();if(0===t.length)return void console.warn("Private part is empty");const e=k(pt.value),n=k(t),r=await dt.encrypt(e,n);yt.value=L(r)}),bt.addEventListener("click",async()=>{if(pt.focus(),yt.value="",Tt(),0===pt.value.length)return void Pt();const t=g();if(0!==t.length)try{const e=M(pt.value),n=k(t),r=await dt.decrypt(e,n);yt.value=function(t){return(new TextDecoder).decode(t)}(r)}catch(t){console.warn(`Failed to decrypt${t.message?`, error: ${t.message}`:", no error message"}`),yt.style.setProperty("background-color",s)}else console.warn("Private part is empty")}),pt.addEventListener("input",()=>{pt.value.length>0&&mt()}),ft.addEventListener("click",()=>{pt.value=""}),wt.addEventListener("click",()=>{yt.value=""});const Et=new Uint8Array([242,207,239,142,19,64,70,73,146,42,222,92,188,136,56,168]).buffer;const Lt=[new class{get version(){return 1}get description(){return"PBKDF2 + AES-GCM"}async encrypt(t,e){const n=new ArrayBuffer(28+t.byteLength),r=new DataView(n,0,12);crypto.getRandomValues(new Uint8Array(n,0,12));const a={name:"AES-GCM",iv:r},o=await crypto.subtle.importKey("raw",await C(e,Et),{name:"AES-GCM",length:256},!1,["encrypt"]),i=await crypto.subtle.encrypt(a,o,t);return new Uint8Array(n).set(new Uint8Array(i),12),n}async decrypt(t,e){const n=new DataView(t,0,12),r=new DataView(t,12),a={name:"AES-GCM",iv:n},o=await C(e,Et),i=await crypto.subtle.importKey("raw",o,{name:"AES-GCM",length:256},!1,["decrypt"]);return await crypto.subtle.decrypt(a,i,r)}},new lt],St=a("txtReEncryptSource"),Ct=a("txtReEncryptTarget"),At=a("cboReEncryptFrom"),kt=a("cboReEncryptTo"),Mt=a("btnReEncrypt"),xt=a("btnClearReEncryptSource"),Ht=a("spnCopyReEncryptTargetFeedback"),Rt=a("btnCopyReEncryptTarget"),It=a("btnClearReEncryptTarget");function Ot(t,e){let n;for(n of Lt){const e=document.createElement("option");e.value=t.childNodes.length.toString(),e.text=`${n.description} (v${n.version})`,t.add(e)}t.value=e.toString()}function Dt(){St.style.removeProperty("background-color")}function Ut(){Dt(),Ct.style.removeProperty("background-color")}o(Ct,Rt,Ht),Ot(At,Lt.length-2),Ot(kt,Lt.length-1),Mt.addEventListener("click",async()=>{if(Ct.value="",Ut(),0===St.value.length)return void St.style.setProperty("background-color",s);if(At.value===kt.value)return void Ct.style.setProperty("background-color",s);const t=g();if(0===t.length)return void console.warn("Private part is empty");const e=parseInt(At.value,10),n=parseInt(kt.value,10),r=k(t),a=M(St.value),o=await Lt[e].decrypt(a,r),i=await Lt[n].encrypt(o,r);Ct.value=L(i)}),St.addEventListener("input",()=>{St.value.length>0&&Dt()}),xt.addEventListener("click",()=>{St.value=""}),It.addEventListener("click",()=>{Ct.value=""});const Ft=a("btnTabNothing"),Kt=a("btnTabPasswords"),jt=a("btnTabCiphers"),Bt=a("btnTabReEncrypt");new class{constructor(t){this.tabs=t,this._activeTabIndex=-1;for(let e=0;e<this.tabs.length;e+=1)t[e].button.addEventListener("click",()=>{this.setActiveTab(e)});this.setActiveTab(0)}get activeTabIndex(){return this._activeTabIndex}set activeTabIndex(t){if(t<0||t>=this.tabs.length)throw new Error(`Argument 'index' out of range. Must be in range [0;${this.tabs.length-1}].`);this.setActiveTab(t)}setActiveTab(t){let e;for(e of this.tabs)e.button.style.removeProperty("font-weight"),e.button.style.setProperty("color","#C0C0C0"),e.content.style.setProperty("display","none");this.tabs[t].button.style.setProperty("font-weight","bold"),this.tabs[t].button.style.removeProperty("color"),this.tabs[t].content.style.removeProperty("display"),this._activeTabIndex=t}}([{button:Ft,content:a("divTabNothing")},{button:Kt,content:a("divTabPasswords")},{button:jt,content:a("divTabCiphers")},{button:Bt,content:a("divTabReEncrypt")}]);const Gt="ae331ebe2755ef77c2878f81db2f96cc96e973d6".substr(0,11);a("divInfo").innerHTML=`${Gt}<br/><a href="https://github.com/TanukiSharp/ItchyPassword" target="_blank">github</a>`}]);
//# sourceMappingURL=bundle.js.map