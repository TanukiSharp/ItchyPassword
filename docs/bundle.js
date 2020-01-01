/******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
/******/ 		}
/******/ 	};
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function(exports) {
/******/ 		if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		}
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function(value, mode) {
/******/ 		if(mode & 1) value = __webpack_require__(value);
/******/ 		if(mode & 8) return value;
/******/ 		if((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if(mode & 2 && typeof value != 'string') for(var key in value) __webpack_require__.d(ns, key, function(key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = "./src/index.ts");
/******/ })
/************************************************************************/
/******/ ({

/***/ "./src/TabControl.ts":
/*!***************************!*\
  !*** ./src/TabControl.ts ***!
  \***************************/
/*! exports provided: TabControl */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "TabControl", function() { return TabControl; });
class TabControl {
    constructor(tabs) {
        this.tabs = tabs;
        this._activeTabIndex = -1;
        for (let i = 0; i < this.tabs.length; i += 1) {
            tabs[i].getTabButton().addEventListener('click', () => {
                this.setActiveTab(i);
            });
        }
        this.setActiveTab(0);
    }
    get activeTabIndex() {
        return this._activeTabIndex;
    }
    set activeTabIndex(index) {
        if (index < 0 || index >= this.tabs.length) {
            throw new Error(`Argument 'index' out of range. Must be in range [0;${this.tabs.length - 1}].`);
        }
        this.setActiveTab(index);
    }
    setActiveTab(activeTabIndex) {
        if (activeTabIndex === this._activeTabIndex) {
            return;
        }
        let tabInfo;
        for (tabInfo of this.tabs) {
            const button = tabInfo.getTabButton();
            button.style.removeProperty('font-weight');
            button.style.setProperty('color', '#C0C0C0');
            tabInfo.getTabContent().style.setProperty('display', 'none');
        }
        const button = this.tabs[activeTabIndex].getTabButton();
        button.style.setProperty('font-weight', 'bold');
        button.style.removeProperty('color');
        this.tabs[activeTabIndex].getTabContent().style.removeProperty('display');
        this._activeTabIndex = activeTabIndex;
        this.tabs[activeTabIndex].onTabSelected();
    }
}


/***/ }),

/***/ "./src/TimedAction.ts":
/*!****************************!*\
  !*** ./src/TimedAction.ts ***!
  \****************************/
/*! exports provided: TimedAction */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "TimedAction", function() { return TimedAction; });
class TimedAction {
    constructor(action, delay) {
        this.action = action;
        this.delay = delay;
    }
    reset(overrideDelay = undefined) {
        if (this.timeout !== undefined) {
            clearTimeout(this.timeout);
        }
        const delay = overrideDelay !== undefined ? overrideDelay : this.delay;
        this.timeout = setTimeout(() => {
            this.action();
            this.timeout = undefined;
        }, delay);
    }
}


/***/ }),

/***/ "./src/VisualFeedback.ts":
/*!*******************************!*\
  !*** ./src/VisualFeedback.ts ***!
  \*******************************/
/*! exports provided: VisualFeedback */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "VisualFeedback", function() { return VisualFeedback; });
class VisualFeedback {
    constructor(element) {
        this.element = element;
    }
    setText(text, duration) {
        this.element.innerHTML = text;
        if (this.timeout) {
            clearTimeout(this.timeout);
        }
        this.timeout = setTimeout(() => this.element.innerHTML = '', duration);
    }
}


/***/ }),

/***/ "./src/arrayUtils.ts":
/*!***************************!*\
  !*** ./src/arrayUtils.ts ***!
  \***************************/
/*! exports provided: arrayToString, copy, unsignedBigIntToArrayBuffer, toCustomBaseOneWay, toCustomBase, fromCustomBase, toBase16 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "arrayToString", function() { return arrayToString; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "copy", function() { return copy; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "unsignedBigIntToArrayBuffer", function() { return unsignedBigIntToArrayBuffer; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "toCustomBaseOneWay", function() { return toCustomBaseOneWay; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "toCustomBase", function() { return toCustomBase; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "fromCustomBase", function() { return fromCustomBase; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "toBase16", function() { return toBase16; });
function arrayToString(array) {
    const decoder = new TextDecoder( /*'utf-8'*/);
    return decoder.decode(array);
}
;
function copy(source, sourceIndex, target, targetIndex, length) {
    for (let i = 0; i < length; i += 1) {
        target[i + targetIndex] = source[i + sourceIndex];
    }
}
function createHeaderedBuffer(buffer) {
    if (buffer.byteLength > 0xFFFF) {
        throw new Error(`Buffer too large: ${buffer.byteLength} bytes`);
    }
    let length = buffer.byteLength;
    const headeredBuffer = new Uint8Array(2 + buffer.byteLength);
    for (let i = 0; i < 2; i += 1) {
        headeredBuffer[i] = length % 256;
        length /= 256;
    }
    headeredBuffer.set(new Uint8Array(buffer), 2);
    return headeredBuffer.buffer;
}
function arrayBufferToUnsignedBigIntWithoutHeader(arrayBuffer) {
    const length = arrayBuffer.byteLength;
    const arrayView = new DataView(arrayBuffer, 0);
    let result = 0n;
    let multiplier = 1n;
    for (let i = 0; i < length; i += 1) {
        result += BigInt(arrayView.getUint8(i)) * multiplier;
        multiplier *= 256n;
    }
    return result;
}
function arrayBufferToUnsignedBigInt(arrayBuffer) {
    arrayBuffer = createHeaderedBuffer(arrayBuffer);
    const length = arrayBuffer.byteLength;
    const arrayView = new DataView(arrayBuffer, 0);
    let result = 0n;
    let multiplier = 1n;
    for (let i = 0; i < length; i += 1) {
        result += BigInt(arrayView.getUint8(i)) * multiplier;
        multiplier *= 256n;
    }
    return result;
}
function unsignedBigIntToArrayBuffer(number) {
    const result = [];
    while (number > 0n) {
        const remainder = number % 256n;
        number /= 256n;
        const byteValue = Number(BigInt.asUintN(8, remainder));
        result.push(byteValue);
    }
    let totalLength = result[0];
    if (result.length > 1) { // For case where original buffer is of length 1 and contains 0.
        totalLength += result[1] * 256;
    }
    // The varable 'result' contains 2 bytes of size header.
    const diff = totalLength - (result.length - 2);
    for (let i = 0; i < diff; i += 1) {
        result.push(0);
    }
    return new Uint8Array(result.slice(2)).buffer;
}
// This is a one way encoding in the sense that decoding is not always deterministic.
// This can be used to generate strings where decoding it doesn't matter.
function toCustomBaseOneWay(bytes, alphabet) {
    const alphabetLength = BigInt(alphabet.length);
    let result = '';
    let number = arrayBufferToUnsignedBigIntWithoutHeader(bytes);
    while (number > 0n) {
        const remainder = number % alphabetLength;
        number /= alphabetLength;
        const index = BigInt.asUintN(8, remainder);
        result += alphabet[index];
    }
    return result;
}
function toCustomBase(bytes, alphabet) {
    const alphabetLength = BigInt(alphabet.length);
    let result = '';
    let number = arrayBufferToUnsignedBigInt(bytes);
    while (number > 0n) {
        const remainder = number % alphabetLength;
        number /= alphabetLength;
        const index = BigInt.asUintN(8, remainder);
        result += alphabet[index];
    }
    return result;
}
function fromCustomBase(input, alphabet) {
    const alphabetLength = BigInt(alphabet.length);
    let number = 0n;
    let multiplier = 1n;
    for (let i = 0; i < input.length; i += 1) {
        const value = BigInt(alphabet.indexOf(input[i]));
        number += value * multiplier;
        multiplier *= alphabetLength;
    }
    return unsignedBigIntToArrayBuffer(number);
}
function toBase16(buffer) {
    return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join('');
}


/***/ }),

/***/ "./src/ciphers/v1.ts":
/*!***************************!*\
  !*** ./src/ciphers/v1.ts ***!
  \***************************/
/*! exports provided: CipherV1 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "CipherV1", function() { return CipherV1; });
/* harmony import */ var _crypto__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../crypto */ "./src/crypto.ts");

const encryptionKeyDerivationSalt = new Uint8Array([0xf2, 0xcf, 0xef, 0x8e, 0x13, 0x40, 0x46, 0x49, 0x92, 0x2a, 0xde, 0x5c, 0xbc, 0x88, 0x38, 0xa8]).buffer;
class CipherV1 {
    get version() {
        return 1;
    }
    get description() {
        return 'PBKDF2 + AES-GCM';
    }
    async encrypt(input, password) {
        const output = new ArrayBuffer(12 + 16 + input.byteLength);
        const nonce = new DataView(output, 0, 12);
        crypto.getRandomValues(new Uint8Array(output, 0, 12));
        const aesGcmParams = {
            name: 'AES-GCM',
            iv: nonce
        };
        const aesKeyAlgorithm = {
            name: 'AES-GCM',
            length: 256
        };
        const passwordKey = await crypto.subtle.importKey('raw', await Object(_crypto__WEBPACK_IMPORTED_MODULE_0__["getDerivedBytes"])(password, encryptionKeyDerivationSalt), aesKeyAlgorithm, false, ['encrypt']);
        const result = await crypto.subtle.encrypt(aesGcmParams, passwordKey, input);
        new Uint8Array(output).set(new Uint8Array(result), 12);
        return output;
    }
    async decrypt(input, password) {
        const nonce = new DataView(input, 0, 12);
        const payload = new DataView(input, 12);
        const aesGcmParams = {
            name: 'AES-GCM',
            iv: nonce
        };
        const aesKeyAlgorithm = {
            name: 'AES-GCM',
            length: 256
        };
        const derivedKey = await Object(_crypto__WEBPACK_IMPORTED_MODULE_0__["getDerivedBytes"])(password, encryptionKeyDerivationSalt);
        const passwordKey = await crypto.subtle.importKey('raw', derivedKey, aesKeyAlgorithm, false, ['decrypt']);
        return await crypto.subtle.decrypt(aesGcmParams, passwordKey, payload);
    }
}


/***/ }),

/***/ "./src/ciphers/v2.ts":
/*!***************************!*\
  !*** ./src/ciphers/v2.ts ***!
  \***************************/
/*! exports provided: CipherV2 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "CipherV2", function() { return CipherV2; });
/* harmony import */ var _crypto__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../crypto */ "./src/crypto.ts");

class CipherV2 {
    get version() {
        return 2;
    }
    get description() {
        return 'PBKDF2 + AES-GCM';
    }
    async encrypt(input, password) {
        const output = new ArrayBuffer(12 + 16 + 16 + input.byteLength);
        const nonce = crypto.getRandomValues(new Uint8Array(output, 0, 12));
        const passwordSalt = crypto.getRandomValues(new Uint8Array(output, 12, 16));
        const aesGcmParams = {
            name: 'AES-GCM',
            iv: nonce
        };
        const aesKeyAlgorithm = {
            name: 'AES-GCM',
            length: 256
        };
        const passwordKey = await crypto.subtle.importKey('raw', await Object(_crypto__WEBPACK_IMPORTED_MODULE_0__["getDerivedBytes"])(password, passwordSalt), aesKeyAlgorithm, false, ['encrypt']);
        const result = await crypto.subtle.encrypt(aesGcmParams, passwordKey, input);
        new Uint8Array(output).set(new Uint8Array(result), 12 + 16);
        return output;
    }
    async decrypt(input, password) {
        const nonce = new Uint8Array(input, 0, 12);
        const passwordSalt = new Uint8Array(input, 12, 16);
        const payload = new Uint8Array(input, 12 + 16);
        const aesGcmParams = {
            name: 'AES-GCM',
            iv: nonce
        };
        const aesKeyAlgorithm = {
            name: 'AES-GCM',
            length: 256
        };
        const derivedKey = await Object(_crypto__WEBPACK_IMPORTED_MODULE_0__["getDerivedBytes"])(password, passwordSalt);
        const passwordKey = await crypto.subtle.importKey('raw', derivedKey, aesKeyAlgorithm, false, ['decrypt']);
        return await crypto.subtle.decrypt(aesGcmParams, passwordKey, payload);
    }
}


/***/ }),

/***/ "./src/components/cipherComponent.ts":
/*!*******************************************!*\
  !*** ./src/components/cipherComponent.ts ***!
  \*******************************************/
/*! exports provided: CipherComponent */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "CipherComponent", function() { return CipherComponent; });
/* harmony import */ var _crypto__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../crypto */ "./src/crypto.ts");
/* harmony import */ var _stringUtils__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../stringUtils */ "./src/stringUtils.ts");
/* harmony import */ var _arrayUtils__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../arrayUtils */ "./src/arrayUtils.ts");
/* harmony import */ var _ui__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../ui */ "./src/ui.ts");
/* harmony import */ var _privatePartComponent__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./privatePartComponent */ "./src/components/privatePartComponent.ts");
/* harmony import */ var _ciphers_v2__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ../ciphers/v2 */ "./src/ciphers/v2.ts");
/* harmony import */ var _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./storageOutputComponent */ "./src/components/storageOutputComponent.ts");







const RESERVED_KEYS = ['version', 'value'];
const btnTabCiphers = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('btnTabCiphers');
const divTabCiphers = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('divTabCiphers');
const cipher = new _ciphers_v2__WEBPACK_IMPORTED_MODULE_5__["CipherV2"]();
const txtCipherName = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('txtCipherName');
const txtCipherSource = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('txtCipherSource');
const txtCipherTarget = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('txtCipherTarget');
const btnEncrypt = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('btnEncrypt');
const btnDecrypt = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('btnDecrypt');
const btnClearCipherSource = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('btnClearCipherSource');
const spnCopyCipherTargetFeedback = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('spnCopyCipherTargetFeedback');
const btnCopyCipherTarget = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('btnCopyCipherTarget');
const btnClearCipherTarget = Object(_ui__WEBPACK_IMPORTED_MODULE_3__["getElementById"])('btnClearCipherTarget');
function clearSourceVisualCue() {
    txtCipherSource.style.removeProperty('background-color');
}
function clearTargetVisualCue() {
    txtCipherTarget.style.removeProperty('background-color');
}
function setSourceVisualCueError() {
    txtCipherSource.style.setProperty('background-color', _ui__WEBPACK_IMPORTED_MODULE_3__["ERROR_COLOR"]);
}
function setTargetVisualCueError() {
    txtCipherTarget.style.setProperty('background-color', _ui__WEBPACK_IMPORTED_MODULE_3__["ERROR_COLOR"]);
}
function clearAllVisualCues() {
    clearSourceVisualCue();
    clearTargetVisualCue();
}
function setCipherTargetValue(value) {
    txtCipherTarget.value = value;
    onCipherTargetChanged();
}
function onCipherTargetChanged() {
    updateCipherParameters();
}
function updateCipherParameters() {
    if (txtCipherTarget.value === '' || txtCipherName.value === '') {
        _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["clearOutputs"]();
        return;
    }
    const cipherParameters = {
        version: cipher.version,
        value: txtCipherTarget.value
    };
    const path = `ciphers/${txtCipherName.value}`;
    _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["setParameters"](cipherParameters, path, RESERVED_KEYS);
}
async function onEncryptButtonClick() {
    txtCipherSource.focus();
    setCipherTargetValue('');
    clearAllVisualCues();
    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return;
    }
    const privatePart = Object(_privatePartComponent__WEBPACK_IMPORTED_MODULE_4__["getPrivatePart"])();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return;
    }
    const input = _stringUtils__WEBPACK_IMPORTED_MODULE_1__["stringToArray"](txtCipherSource.value);
    const password = _stringUtils__WEBPACK_IMPORTED_MODULE_1__["stringToArray"](privatePart);
    const encrypted = await cipher.encrypt(input, password);
    setCipherTargetValue(_arrayUtils__WEBPACK_IMPORTED_MODULE_2__["toCustomBase"](encrypted, _crypto__WEBPACK_IMPORTED_MODULE_0__["BASE62_ALPHABET"]));
}
async function onDecryptButtonClick() {
    txtCipherSource.focus();
    setCipherTargetValue('');
    clearAllVisualCues();
    if (txtCipherSource.value.length === 0) {
        setSourceVisualCueError();
        return;
    }
    const privatePart = Object(_privatePartComponent__WEBPACK_IMPORTED_MODULE_4__["getPrivatePart"])();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return;
    }
    try {
        const input = _arrayUtils__WEBPACK_IMPORTED_MODULE_2__["fromCustomBase"](txtCipherSource.value, _crypto__WEBPACK_IMPORTED_MODULE_0__["BASE62_ALPHABET"]);
        const password = _stringUtils__WEBPACK_IMPORTED_MODULE_1__["stringToArray"](privatePart);
        const decrypted = await cipher.decrypt(input, password);
        setCipherTargetValue(_arrayUtils__WEBPACK_IMPORTED_MODULE_2__["arrayToString"](decrypted));
    }
    catch (error) {
        console.warn(`Failed to decrypt${error.message ? `, error: ${error.message}` : ', no error message'}`);
        setTargetVisualCueError();
    }
}
class CipherComponent {
    getTabButton() {
        return btnTabCiphers;
    }
    getTabContent() {
        return divTabCiphers;
    }
    onTabSelected() {
        _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["show"]();
        updateCipherParameters();
    }
    init() {
        Object(_ui__WEBPACK_IMPORTED_MODULE_3__["setupCopyButton"])(txtCipherTarget, btnCopyCipherTarget, spnCopyCipherTargetFeedback);
        btnEncrypt.addEventListener('click', onEncryptButtonClick);
        btnDecrypt.addEventListener('click', onDecryptButtonClick);
        txtCipherName.addEventListener('input', () => {
            updateCipherParameters();
        });
        txtCipherSource.addEventListener('input', () => {
            if (txtCipherSource.value.length > 0) {
                clearSourceVisualCue();
            }
        });
        btnClearCipherSource.addEventListener('click', () => {
            txtCipherSource.value = '';
        });
        btnClearCipherTarget.addEventListener('click', () => {
            setCipherTargetValue('');
        });
    }
}


/***/ }),

/***/ "./src/components/passwordComponent.ts":
/*!*********************************************!*\
  !*** ./src/components/passwordComponent.ts ***!
  \*********************************************/
/*! exports provided: PasswordComponent */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "PasswordComponent", function() { return PasswordComponent; });
/* harmony import */ var _ui__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../ui */ "./src/ui.ts");
/* harmony import */ var _privatePartComponent__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./privatePartComponent */ "./src/components/privatePartComponent.ts");
/* harmony import */ var _crypto__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../crypto */ "./src/crypto.ts");
/* harmony import */ var _stringUtils__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../stringUtils */ "./src/stringUtils.ts");
/* harmony import */ var _arrayUtils__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ../arrayUtils */ "./src/arrayUtils.ts");
/* harmony import */ var _passwordGenerators_v1__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ../passwordGenerators/v1 */ "./src/passwordGenerators/v1.ts");
/* harmony import */ var _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./storageOutputComponent */ "./src/components/storageOutputComponent.ts");







const btnTabPasswords = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnTabPasswords');
const divTabPasswords = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('divTabPasswords');
const passwordGenerator = new _passwordGenerators_v1__WEBPACK_IMPORTED_MODULE_5__["PasswordGeneratorV1"]('Password');
const txtPublicPart = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtPublicPart');
const btnGeneratePublicPart = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnGeneratePublicPart');
const btnClearPublicPart = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnClearPublicPart');
const btnCopyPublicPart = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnCopyPublicPart');
const spnCopyPublicPartFeedback = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnCopyPublicPartFeedback');
const numOutputSizeRange = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('numOutputSizeRange');
const numOutputSizeNum = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('numOutputSizeNum');
const txtAlphabet = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtAlphabet');
const spnAlphabetSize = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnAlphabetSize');
const btnResetAlphabet = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnResetAlphabet');
const txtResultPassword = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtResultPassword');
const spnResultPasswordLength = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnResultPasswordLength');
const btnCopyResultPassword = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnCopyResultPassword');
const spnCopyResultPasswordFeedback = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnCopyResultPasswordFeedback');
const DEFAULT_LENGTH = 64;
const DEFAULT_ALPHABET = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';
const RESERVED_KEYS = ['alphabet', 'length', 'public', 'datetime'];
let passwordPublicPartLastChange;
function onClearPublicPartButtonClick() {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to clear the public part ?\nType \'y\' to accept', '') !== 'y') {
            return;
        }
    }
    txtPublicPart.value = '';
    updatePasswordPublicPartLastUpdate();
    updatePasswordGenerationParameters();
}
function onGeneratePublicPartButtonClick() {
    if (txtPublicPart.value.length > 0) {
        if (prompt('Are you sure you want to generate a new public part ?\nType \'y\' to accept', '') !== 'y') {
            return;
        }
    }
    const randomString = _crypto__WEBPACK_IMPORTED_MODULE_2__["generateRandomString"]();
    txtPublicPart.value = randomString;
    updatePasswordPublicPartLastUpdate();
    run();
}
function updatePasswordPublicPartLastUpdate() {
    if (txtPublicPart.value.length > 0) {
        passwordPublicPartLastChange = new Date().toISOString();
    }
    else {
        passwordPublicPartLastChange = undefined;
    }
}
function deepMerge(source, target) {
    for (const sourceKey of Object.keys(source)) {
        const targetValue = target[sourceKey];
        const sourceValue = source[sourceKey];
        if (targetValue === undefined ||
            targetValue === null ||
            targetValue.constructor.name !== 'Object' ||
            sourceValue.constructor.name !== 'Object') {
            target[sourceKey] = sourceValue;
            continue;
        }
        deepMerge(sourceValue, targetValue);
    }
}
function setupViewButton(txt, buttonName) {
    const btn = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])(buttonName);
    btn.addEventListener('click', () => {
        if (txt.type === 'password') {
            txt.type = 'input';
            btn.innerHTML = 'Hide';
        }
        else {
            txt.type = 'password';
            btn.innerHTML = 'View';
        }
    });
}
function updateResultPasswordLength() {
    spnResultPasswordLength.innerHTML = txtResultPassword.value.length.toString().padStart(2, ' ');
}
function isAlphabetValid(alphabet) {
    const sortedAlphabet = alphabet.split('');
    sortedAlphabet.sort();
    for (let i = 1; i < sortedAlphabet.length; i += 1) {
        if (sortedAlphabet[i - 1] === sortedAlphabet[i]) {
            return false;
        }
    }
    return true;
}
function updatePasswordGenerationParameters() {
    if (canRun() === false) {
        clearOutputs();
        return;
    }
    const passwordParamters = {
        public: txtPublicPart.value,
        datetime: passwordPublicPartLastChange
    };
    const numericValue = txtResultPassword.value.length;
    if (numericValue !== DEFAULT_LENGTH) {
        passwordParamters.length = numericValue;
    }
    const alphabet = txtAlphabet.value;
    if (alphabet !== DEFAULT_ALPHABET) {
        passwordParamters.alphabet = alphabet;
    }
    _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["setParameters"](passwordParamters, 'password', RESERVED_KEYS);
}
function updateOutputSizeRangeToNum() {
    numOutputSizeNum.value = numOutputSizeRange.value;
}
function updateOutputSizeNumToRange() {
    const min = parseInt(numOutputSizeRange.min, 10);
    const val = parseInt(numOutputSizeNum.value, 10);
    const max = parseInt(numOutputSizeRange.max, 10);
    if (isNaN(val) === false) {
        numOutputSizeRange.value = Math.max(min, Math.min(val, max)).toString();
        return true;
    }
    return false;
}
async function onOutputSizeRangeInput() {
    updateOutputSizeRangeToNum();
    await run();
}
async function onOutputSizeNumInput() {
    if (updateOutputSizeNumToRange()) {
        updateOutputSizeRangeToNum();
    }
    await run();
}
function updateAlphabetSize() {
    spnAlphabetSize.innerHTML = txtAlphabet.value.length.toString();
    const alphabetSizeDigitCount = txtAlphabet.value.length.toString().length;
    if (alphabetSizeDigitCount < 2) {
        // Add a space to keep a nice visual alignment.
        spnAlphabetSize.innerHTML = spnAlphabetSize.innerHTML.padStart(2, ' ');
    }
}
function updateAlphabetValidityDisplay(isAlphabetValid) {
    if (isAlphabetValid) {
        txtAlphabet.style.removeProperty('background');
    }
    else {
        txtAlphabet.style.setProperty('background', _ui__WEBPACK_IMPORTED_MODULE_0__["ERROR_COLOR"]);
    }
}
async function onAlphabetTextInput() {
    const isAlphabetValidResult = isAlphabetValid(txtAlphabet.value);
    updateAlphabetValidityDisplay(isAlphabetValidResult);
    if (isAlphabetValidResult === false) {
        return;
    }
    updateAlphabetSize();
    await run();
}
async function onResetAlphabetButtonClick() {
    resetAlphabet();
    updateAlphabetSize();
    await run();
}
function clearOutputs() {
    txtResultPassword.value = '';
    _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["clearOutputs"]();
    updateResultPasswordLength();
}
function canRun() {
    const alphabet = txtAlphabet.value;
    if (isAlphabetValid(alphabet) === false) {
        return false;
    }
    if (_privatePartComponent__WEBPACK_IMPORTED_MODULE_1__["getPrivatePart"]().length <= 0 || txtPublicPart.value.length < 8 || alphabet.length < 2) {
        return false;
    }
    return true;
}
async function run() {
    if (canRun() === false) {
        clearOutputs();
        return;
    }
    const privatePartString = _privatePartComponent__WEBPACK_IMPORTED_MODULE_1__["getPrivatePart"]();
    const publicPartString = txtPublicPart.value;
    const privatePrivateBytes = _stringUtils__WEBPACK_IMPORTED_MODULE_3__["stringToArray"](privatePartString);
    const publicPartBytes = _stringUtils__WEBPACK_IMPORTED_MODULE_3__["stringToArray"](publicPartString);
    const keyBytes = await passwordGenerator.generatePassword(privatePrivateBytes, publicPartBytes);
    const keyString = _arrayUtils__WEBPACK_IMPORTED_MODULE_4__["toCustomBaseOneWay"](keyBytes, txtAlphabet.value);
    txtResultPassword.value = _stringUtils__WEBPACK_IMPORTED_MODULE_3__["truncate"](keyString, Math.max(4, parseInt(numOutputSizeRange.value, 10)));
    updateResultPasswordLength();
    updatePasswordGenerationParameters();
}
async function resetAlphabet() {
    txtAlphabet.value = DEFAULT_ALPHABET;
    updateAlphabetSize();
    const isAlphabetValidResult = isAlphabetValid(txtAlphabet.value);
    updateAlphabetValidityDisplay(isAlphabetValidResult);
    if (isAlphabetValidResult) {
        await run();
    }
}
async function onPublicPartTextInput() {
    updatePasswordPublicPartLastUpdate();
    await run();
}
class PasswordComponent {
    getTabButton() {
        return btnTabPasswords;
    }
    getTabContent() {
        return divTabPasswords;
    }
    onTabSelected() {
        _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["show"]();
        updatePasswordGenerationParameters();
    }
    init() {
        _privatePartComponent__WEBPACK_IMPORTED_MODULE_1__["registerOnChanged"](run);
        // dafuq!?
        numOutputSizeRange.max = DEFAULT_LENGTH.toString();
        numOutputSizeRange.value = DEFAULT_LENGTH.toString();
        btnClearPublicPart.addEventListener('click', onClearPublicPartButtonClick);
        btnGeneratePublicPart.addEventListener('click', onGeneratePublicPartButtonClick);
        setupViewButton(txtResultPassword, 'btnViewResultPassword');
        Object(_ui__WEBPACK_IMPORTED_MODULE_0__["setupCopyButton"])(txtPublicPart, btnCopyPublicPart, spnCopyPublicPartFeedback);
        Object(_ui__WEBPACK_IMPORTED_MODULE_0__["setupCopyButton"])(txtResultPassword, btnCopyResultPassword, spnCopyResultPasswordFeedback);
        numOutputSizeRange.addEventListener('input', onOutputSizeRangeInput);
        numOutputSizeNum.addEventListener('input', onOutputSizeNumInput);
        txtAlphabet.addEventListener('input', onAlphabetTextInput);
        btnResetAlphabet.addEventListener('click', onResetAlphabetButtonClick);
        txtPublicPart.addEventListener('input', onPublicPartTextInput);
        updateOutputSizeRangeToNum();
        resetAlphabet();
    }
}
;


/***/ }),

/***/ "./src/components/privatePartComponent.ts":
/*!************************************************!*\
  !*** ./src/components/privatePartComponent.ts ***!
  \************************************************/
/*! exports provided: registerOnChanged, getPrivatePart, PrivatePartComponent */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "registerOnChanged", function() { return registerOnChanged; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "getPrivatePart", function() { return getPrivatePart; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "PrivatePartComponent", function() { return PrivatePartComponent; });
/* harmony import */ var _ui__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../ui */ "./src/ui.ts");
/* harmony import */ var _TimedAction__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../TimedAction */ "./src/TimedAction.ts");


const btnProtectTitleForProtect = 'Stores the string in memory and removes it from the UI component. Prevents a physical intruder from copy/pasting the value.';
const btnProtectTitleForClear = 'Removes the string form memory and re-enables the UI component.';
const txtPrivatePart = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtPrivatePart');
const txtPrivatePartConfirmation = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtPrivatePartConfirmation');
const btnProtect = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnProtect');
const spnProtectedConfirmation = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnProtectedConfirmation');
const spnPrivatePartSize = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnPrivatePartSize');
const spnPrivatePartSizeConfirmation = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('spnPrivatePartSizeConfirmation');
const PRIVATE_PART_PROTECTION_TIMEOUT = 60 * 1000;
let privatePart;
const onChangedHandlers = [];
function registerOnChanged(onChanged) {
    onChangedHandlers.push(onChanged);
}
function getPrivatePart() {
    if (privatePart !== undefined) {
        return privatePart;
    }
    return txtPrivatePart.value;
}
function protectAndLockPrivatePart() {
    if (txtPrivatePart.value.length === 0) {
        return;
    }
    privatePart = txtPrivatePart.value;
    spnProtectedConfirmation.innerHTML = 'Protected';
    txtPrivatePart.value = '';
    txtPrivatePartConfirmation.value = '';
    spnPrivatePartSize.innerHTML = '0';
    spnPrivatePartSizeConfirmation.innerHTML = '0';
    txtPrivatePart.disabled = true;
    txtPrivatePartConfirmation.disabled = true;
    btnProtect.innerHTML = 'Clear and unlock';
    btnProtect.title = btnProtectTitleForClear;
    updatePrivatePartsMatching();
}
function clearAndUnLockPrivatePart() {
    privatePart = undefined;
    spnProtectedConfirmation.innerHTML = '';
    txtPrivatePart.disabled = false;
    txtPrivatePartConfirmation.disabled = false;
    btnProtect.innerHTML = 'Protect and lock';
    btnProtect.title = btnProtectTitleForProtect;
    btnProtect.disabled = true;
}
function togglePrivatePartProtection() {
    if (privatePart === undefined) {
        protectAndLockPrivatePart();
    }
    else {
        clearAndUnLockPrivatePart();
    }
}
function onProtectButtonClick() {
    togglePrivatePartProtection();
}
const protectPrivatePartAction = new _TimedAction__WEBPACK_IMPORTED_MODULE_1__["TimedAction"](protectAndLockPrivatePart, PRIVATE_PART_PROTECTION_TIMEOUT);
function onPrivatePartTextInput() {
    btnProtect.disabled = txtPrivatePart.value.length === 0;
    spnPrivatePartSize.innerHTML = txtPrivatePart.value.length.toString();
    updatePrivatePartsMatching();
    let onChangedHandler;
    for (onChangedHandler of onChangedHandlers) {
        onChangedHandler();
    }
    protectPrivatePartAction.reset();
}
function updatePrivatePartsMatching() {
    if (txtPrivatePartConfirmation.value === txtPrivatePart.value) {
        txtPrivatePartConfirmation.style.setProperty('background', _ui__WEBPACK_IMPORTED_MODULE_0__["SUCCESS_COLOR"]);
    }
    else {
        txtPrivatePartConfirmation.style.setProperty('background', _ui__WEBPACK_IMPORTED_MODULE_0__["ERROR_COLOR"]);
    }
}
;
function onPrivatePartConfirmationTextInput() {
    spnPrivatePartSizeConfirmation.innerHTML = txtPrivatePartConfirmation.value.length.toString();
    protectPrivatePartAction.reset();
    updatePrivatePartsMatching();
}
class PrivatePartComponent {
    init() {
        btnProtect.addEventListener('click', onProtectButtonClick);
        txtPrivatePart.addEventListener('input', onPrivatePartTextInput);
        txtPrivatePartConfirmation.addEventListener('input', onPrivatePartConfirmationTextInput);
        updatePrivatePartsMatching();
        btnProtect.title = btnProtectTitleForProtect;
    }
}


/***/ }),

/***/ "./src/components/reEncryptComponent.ts":
/*!**********************************************!*\
  !*** ./src/components/reEncryptComponent.ts ***!
  \**********************************************/
/*! exports provided: ReEncryptComponent */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "ReEncryptComponent", function() { return ReEncryptComponent; });
/* harmony import */ var _stringUtils__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../stringUtils */ "./src/stringUtils.ts");
/* harmony import */ var _arrayUtils__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../arrayUtils */ "./src/arrayUtils.ts");
/* harmony import */ var _ui__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../ui */ "./src/ui.ts");
/* harmony import */ var _privatePartComponent__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./privatePartComponent */ "./src/components/privatePartComponent.ts");
/* harmony import */ var _ciphers_v1__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ../ciphers/v1 */ "./src/ciphers/v1.ts");
/* harmony import */ var _ciphers_v2__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ../ciphers/v2 */ "./src/ciphers/v2.ts");
/* harmony import */ var _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./storageOutputComponent */ "./src/components/storageOutputComponent.ts");







const ciphers = [
    new _ciphers_v1__WEBPACK_IMPORTED_MODULE_4__["CipherV1"](),
    new _ciphers_v2__WEBPACK_IMPORTED_MODULE_5__["CipherV2"]()
];
const btnTabReEncrypt = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('btnTabReEncrypt');
const divTabReEncrypt = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('divTabReEncrypt');
const txtReEncryptSource = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('txtReEncryptSource');
const txtReEncryptTarget = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('txtReEncryptTarget');
const cboReEncryptFrom = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('cboReEncryptFrom');
const cboReEncryptTo = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('cboReEncryptTo');
const btnReEncrypt = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('btnReEncrypt');
const btnClearReEncryptSource = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('btnClearReEncryptSource');
const spnCopyReEncryptTargetFeedback = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('spnCopyReEncryptTargetFeedback');
const btnCopyReEncryptTarget = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('btnCopyReEncryptTarget');
const btnClearReEncryptTarget = Object(_ui__WEBPACK_IMPORTED_MODULE_2__["getElementById"])('btnClearReEncryptTarget');
function fillCipherComboBox(cbo, initialValue) {
    let cipher;
    for (cipher of ciphers) {
        const item = document.createElement('option');
        item.value = cbo.childNodes.length.toString();
        item.text = `${cipher.description} (v${cipher.version})`;
        cbo.add(item);
    }
    cbo.value = initialValue.toString();
}
function clearSourceVisualCue() {
    txtReEncryptSource.style.removeProperty('background-color');
}
function clearTargetVisualCue() {
    txtReEncryptTarget.style.removeProperty('background-color');
}
function setSourceVisualCueError() {
    txtReEncryptSource.style.setProperty('background-color', _ui__WEBPACK_IMPORTED_MODULE_2__["ERROR_COLOR"]);
}
function setTargetVisualCueError() {
    txtReEncryptTarget.style.setProperty('background-color', _ui__WEBPACK_IMPORTED_MODULE_2__["ERROR_COLOR"]);
}
function clearAllVisualCues() {
    clearSourceVisualCue();
    clearTargetVisualCue();
}
async function onReEncryptButtonClick() {
    txtReEncryptTarget.value = '';
    clearAllVisualCues();
    if (txtReEncryptSource.value.length === 0) {
        setSourceVisualCueError();
        return;
    }
    if (cboReEncryptFrom.value === cboReEncryptTo.value) {
        setTargetVisualCueError();
        return;
    }
    const privatePart = Object(_privatePartComponent__WEBPACK_IMPORTED_MODULE_3__["getPrivatePart"])();
    if (privatePart.length === 0) {
        console.warn('Private part is empty');
        return;
    }
    const sourceCipherIndex = parseInt(cboReEncryptFrom.value, 10);
    const targetCipherIndex = parseInt(cboReEncryptTo.value, 10);
    const password = _stringUtils__WEBPACK_IMPORTED_MODULE_0__["stringToArray"](privatePart);
    const input = _stringUtils__WEBPACK_IMPORTED_MODULE_0__["fromBase16"](txtReEncryptSource.value);
    const decrypted = await ciphers[sourceCipherIndex].decrypt(input, password);
    const reEncrypted = await ciphers[targetCipherIndex].encrypt(decrypted, password);
    txtReEncryptTarget.value = _arrayUtils__WEBPACK_IMPORTED_MODULE_1__["toBase16"](reEncrypted);
}
class ReEncryptComponent {
    getTabButton() {
        return btnTabReEncrypt;
    }
    getTabContent() {
        return divTabReEncrypt;
    }
    onTabSelected() {
        _storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["hide"]();
    }
    init() {
        Object(_ui__WEBPACK_IMPORTED_MODULE_2__["setupCopyButton"])(txtReEncryptTarget, btnCopyReEncryptTarget, spnCopyReEncryptTargetFeedback);
        // Mais est-ce que ce monde est serieux?
        fillCipherComboBox(cboReEncryptFrom, ciphers.length - 2);
        fillCipherComboBox(cboReEncryptTo, ciphers.length - 1);
        txtReEncryptSource.addEventListener('input', () => {
            if (txtReEncryptSource.value.length > 0) {
                clearSourceVisualCue();
            }
        });
        btnClearReEncryptSource.addEventListener('click', () => {
            txtReEncryptSource.value = '';
        });
        btnClearReEncryptTarget.addEventListener('click', () => {
            txtReEncryptTarget.value = '';
        });
        btnReEncrypt.addEventListener('click', onReEncryptButtonClick);
    }
}


/***/ }),

/***/ "./src/components/storageOutputComponent.ts":
/*!**************************************************!*\
  !*** ./src/components/storageOutputComponent.ts ***!
  \**************************************************/
/*! exports provided: clearOutputs, setParameters, show, hide, StorageOutputComponent */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "clearOutputs", function() { return clearOutputs; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "setParameters", function() { return setParameters; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "show", function() { return show; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "hide", function() { return hide; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "StorageOutputComponent", function() { return StorageOutputComponent; });
/* harmony import */ var _ui__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../ui */ "./src/ui.ts");

const divStorageOutput = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('divStorageOutput');
const txtPath = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtPath');
const txtParameters = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtParameters');
const txtCustomKeys = Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('txtCustomKeys');
function shallowMerge(source, target, reservedKeys) {
    const result = {};
    if (source !== null) {
        for (const [key, value] of Object.entries(source)) {
            if (reservedKeys.includes(key) === false) {
                result[key] = value;
            }
        }
    }
    if (target !== null) {
        for (const [key, value] of Object.entries(target)) {
            result[key] = value;
        }
    }
    return result;
}
// Transforms a path like "a/b/c/d" into a hierarchy of objects like { "a": { "b": { "c": { "d": {} } } } }
// From the result object, head is the root object that contains "a", tail is the value of "d", and tailParent is the value of "c"
function pathToObjectChain(path, chainInfo = undefined) {
    const separatorIndex = path.indexOf('/');
    const tail = {};
    const firstPath = separatorIndex >= 0 ? path.substr(0, separatorIndex) : path;
    const remainingPath = separatorIndex >= 0 ? path.substr(separatorIndex + 1) : undefined;
    if (chainInfo === undefined) {
        const node = {};
        node[firstPath] = tail;
        chainInfo = {
            head: node,
            tailParent: node,
            tail
        };
    }
    else {
        chainInfo.tail[firstPath] = tail;
        chainInfo.tailParent = chainInfo.tail;
        chainInfo.tail = tail;
    }
    if (remainingPath) {
        return pathToObjectChain(remainingPath, chainInfo);
    }
    return chainInfo;
}
function onPathTextInput() {
    update();
}
function onCustomKeysTextInput() {
    update();
}
function updateCustomKeysDisplay(isValid) {
    if (isValid) {
        txtCustomKeys.style.removeProperty('background');
        return;
    }
    txtCustomKeys.style.setProperty('background', _ui__WEBPACK_IMPORTED_MODULE_0__["ERROR_COLOR"]);
}
function parseCustomKeys() {
    if (txtCustomKeys.value === '') {
        return {};
    }
    try {
        const obj = JSON.parse(txtCustomKeys.value);
        if (obj === null || obj.constructor.name !== 'Object') {
            return null;
        }
        return obj;
    }
    catch {
        return null;
    }
}
function update() {
    if (_parameterKeys === undefined || _parameterPath === undefined || _reservedKeys === undefined) {
        return;
    }
    const chainInfo = pathToObjectChain(`${txtPath.value}/${_parameterPath}`);
    const leaf = chainInfo.tail;
    for (const [key, value] of Object.entries(_parameterKeys)) {
        leaf[key] = value;
    }
    const customKeys = parseCustomKeys();
    updateCustomKeysDisplay(customKeys !== null);
    const resultParameters = shallowMerge(customKeys, leaf, _reservedKeys);
    if (Object.keys(resultParameters).length === 0) {
        // Set the value of the first (single) property of the object to null.
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = null;
    }
    else {
        chainInfo.tailParent[Object.keys(chainInfo.tailParent)[0]] = resultParameters;
    }
    txtParameters.value = JSON.stringify(chainInfo.head, undefined, 4);
}
function clearOutputs() {
    _parameterKeys = undefined;
    _parameterPath = undefined;
    _reservedKeys = undefined;
    txtParameters.value = '';
}
let _parameterKeys;
let _parameterPath;
let _reservedKeys;
function setParameters(parameterKeys, parameterPath, reservedKeys) {
    _parameterKeys = parameterKeys;
    _parameterPath = parameterPath;
    _reservedKeys = reservedKeys;
    update();
}
function show() {
    divStorageOutput.style.setProperty('display', 'initial');
}
function hide() {
    divStorageOutput.style.setProperty('display', 'none');
}
class StorageOutputComponent {
    init() {
        txtCustomKeys.addEventListener('input', onCustomKeysTextInput);
        txtPath.addEventListener('input', onPathTextInput);
    }
}


/***/ }),

/***/ "./src/crypto.ts":
/*!***********************!*\
  !*** ./src/crypto.ts ***!
  \***********************/
/*! exports provided: BASE62_ALPHABET, getDerivedBytes, generateRandomBytes, generateRandomString */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "BASE62_ALPHABET", function() { return BASE62_ALPHABET; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "getDerivedBytes", function() { return getDerivedBytes; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "generateRandomBytes", function() { return generateRandomBytes; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "generateRandomString", function() { return generateRandomString; });
/* harmony import */ var _arrayUtils__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./arrayUtils */ "./src/arrayUtils.ts");

const BASE62_ALPHABET = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
async function getDerivedBytes(password, salt) {
    const baseKey = await crypto.subtle.importKey('raw', password, 'PBKDF2', false, ['deriveKey']);
    const algorithm = {
        name: 'PBKDF2',
        hash: 'SHA-512',
        iterations: 100000,
        salt
    };
    const derivedKeyType = {
        name: 'AES-CBC',
        length: 256
    };
    const result = await crypto.subtle.deriveKey(algorithm, baseKey, derivedKeyType, true, ['encrypt']);
    const key = await crypto.subtle.exportKey('raw', result);
    return key;
}
function generateRandomBytes(byteCount = 64) {
    const array = new Uint8Array(byteCount);
    return crypto.getRandomValues(array).buffer;
}
function generateRandomString(byteCount = 64, alphabet = BASE62_ALPHABET) {
    const array = generateRandomBytes(byteCount);
    return _arrayUtils__WEBPACK_IMPORTED_MODULE_0__["toCustomBaseOneWay"](array, alphabet);
}


/***/ }),

/***/ "./src/index.ts":
/*!**********************!*\
  !*** ./src/index.ts ***!
  \**********************/
/*! no exports provided */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _ui__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ui */ "./src/ui.ts");
/* harmony import */ var _TabControl__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./TabControl */ "./src/TabControl.ts");
/* harmony import */ var _components_privatePartComponent__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./components/privatePartComponent */ "./src/components/privatePartComponent.ts");
/* harmony import */ var _components_passwordComponent__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./components/passwordComponent */ "./src/components/passwordComponent.ts");
/* harmony import */ var _components_cipherComponent__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./components/cipherComponent */ "./src/components/cipherComponent.ts");
/* harmony import */ var _components_reEncryptComponent__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./components/reEncryptComponent */ "./src/components/reEncryptComponent.ts");
/* harmony import */ var _components_storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./components/storageOutputComponent */ "./src/components/storageOutputComponent.ts");







const nothingTabInfo = {
    getTabButton() {
        return Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('btnTabNothing');
    },
    getTabContent() {
        return Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('divTabNothing');
    },
    onTabSelected() {
        _components_storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["hide"]();
    }
};
const elements = [
    nothingTabInfo,
    new _components_privatePartComponent__WEBPACK_IMPORTED_MODULE_2__["PrivatePartComponent"](),
    new _components_passwordComponent__WEBPACK_IMPORTED_MODULE_3__["PasswordComponent"](),
    new _components_cipherComponent__WEBPACK_IMPORTED_MODULE_4__["CipherComponent"](),
    new _components_reEncryptComponent__WEBPACK_IMPORTED_MODULE_5__["ReEncryptComponent"](),
    new _components_storageOutputComponent__WEBPACK_IMPORTED_MODULE_6__["StorageOutputComponent"](),
];
const tabs = elements.filter(e => e.getTabButton !== undefined);
const components = elements.filter(e => e.init !== undefined);
new _TabControl__WEBPACK_IMPORTED_MODULE_1__["TabControl"](tabs);
const version = "14e6b862af11053d684d93de480af6f4100113bc".substr(0, 11);
const githubLink = '<a href="https://github.com/TanukiSharp/ItchyPassword" target="_blank">github</a>';
Object(_ui__WEBPACK_IMPORTED_MODULE_0__["getElementById"])('divInfo').innerHTML = `${version}<br/>${githubLink}`;
let component;
for (component of components) {
    component.init();
}


/***/ }),

/***/ "./src/passwordGenerators/v1.ts":
/*!**************************************!*\
  !*** ./src/passwordGenerators/v1.ts ***!
  \**************************************/
/*! exports provided: PasswordGeneratorV1 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "PasswordGeneratorV1", function() { return PasswordGeneratorV1; });
/* harmony import */ var _stringUtils__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../stringUtils */ "./src/stringUtils.ts");
/* harmony import */ var _crypto__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../crypto */ "./src/crypto.ts");


class PasswordGeneratorV1 {
    constructor(hkdfPurpose) {
        this.hkdfPurpose = Object(_stringUtils__WEBPACK_IMPORTED_MODULE_0__["stringToArray"])(hkdfPurpose);
        this._description = `HKDF(PBKDF2, HMAC512) [purpose: ${hkdfPurpose}]`;
    }
    get version() {
        return 1;
    }
    get description() {
        return this._description;
    }
    async generatePassword(privatePart, publicPart) {
        const derivedKey = await Object(_crypto__WEBPACK_IMPORTED_MODULE_1__["getDerivedBytes"])(privatePart, publicPart);
        const hmacParameters = {
            name: 'HMAC',
            hash: { name: 'SHA-512' }
        };
        const hkdfKey = await crypto.subtle.importKey('raw', derivedKey, hmacParameters, false, ['sign']);
        return await crypto.subtle.sign('HMAC', hkdfKey, this.hkdfPurpose);
    }
}


/***/ }),

/***/ "./src/stringUtils.ts":
/*!****************************!*\
  !*** ./src/stringUtils.ts ***!
  \****************************/
/*! exports provided: truncate, stringToArray, fromBase16 */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "truncate", function() { return truncate; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "stringToArray", function() { return stringToArray; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "fromBase16", function() { return fromBase16; });
function truncate(input, length) {
    if (input.length <= length) {
        return input;
    }
    return input.substr(0, length);
}
function stringToArray(str) {
    const encoder = new TextEncoder( /*'utf-8'*/);
    return encoder.encode(str).buffer;
}
function fromBase16(str) {
    if (str.length % 2 !== 0) {
        str = '0' + str;
    }
    const result = new Uint8Array(str.length / 2);
    for (let i = 0; i < result.byteLength; i += 1) {
        result[i] = parseInt(str.substr(i * 2, 2), 16);
    }
    return result.buffer;
}


/***/ }),

/***/ "./src/ui.ts":
/*!*******************!*\
  !*** ./src/ui.ts ***!
  \*******************/
/*! exports provided: getElementById, setupCopyButton, SUCCESS_COLOR, ERROR_COLOR */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "getElementById", function() { return getElementById; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "setupCopyButton", function() { return setupCopyButton; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "SUCCESS_COLOR", function() { return SUCCESS_COLOR; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "ERROR_COLOR", function() { return ERROR_COLOR; });
/* harmony import */ var _VisualFeedback__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./VisualFeedback */ "./src/VisualFeedback.ts");

function getElementById(elementName) {
    const element = document.getElementById(elementName);
    if (elementName === null) {
        throw new Error(`DOM element '${elementName}' not found.`);
    }
    return element;
}
async function writeToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    }
    catch (error) {
        console.error(error.stack || error);
        return false;
    }
}
function setupCopyButton(txt, button, feedback) {
    const visualFeedback = new _VisualFeedback__WEBPACK_IMPORTED_MODULE_0__["VisualFeedback"](feedback);
    button.addEventListener('click', async () => {
        if (await writeToClipboard(txt.value)) {
            visualFeedback.setText('Copied', 3000);
        }
        else {
            visualFeedback.setText('<span style="color: red">Failed to copy</span>', 3000);
        }
    });
}
const SUCCESS_COLOR = '#D0FFD0';
const ERROR_COLOR = '#FFD0D0';


/***/ })

/******/ });
//# sourceMappingURL=bundle.js.map