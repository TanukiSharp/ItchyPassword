// import * as crypto from './crypto';
// import * as arrayUtils from './arrayUtils';

// const defaultAlphabet: string = '!"#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~';

// async function postData(url: string = '', data: object = {}): Promise<string> {
//     const response: Response = await fetch(url, {
//         method: 'POST',
//         cache: 'no-cache',
//         headers: {
//             'Content-Type': 'application/json'
//         },
//         body: JSON.stringify(data)
//     });

//     return response.text() as Promise<string>;
// }

// function generateRandomString(alphabet: string): string {
//     const size: number = Math.random() * 8 + 24;

//     let result: string = '';

//     for (let i: number = 0; i < size; i += 1) {
//         const index: number = Math.floor(Math.random() * alphabet.length);
//         result += alphabet[index];
//     }

//     return result;
// }

// async function validate(): Promise<void> {
//     for (let i: number = 0; i < 150; i += 1) {
//         const privateKey: string = generateRandomString(defaultAlphabet);
//         const publicKey: string = generateRandomString(defaultAlphabet);

//         const localDerivedBytesTask: Promise<ArrayBuffer> = crypto.generatePassword(privateKey, publicKey);

//         const remoteDerivedKeyTask: Promise<string> = postData('http://localhost:5000/', {
//             privateKey,
//             publicKey,
//             iterations: 100000,
//             algorithmName: "SHA512",
//             alphabet: defaultAlphabet
//         });

//         await Promise.all([localDerivedBytesTask, remoteDerivedKeyTask]);

//         const localDerivedBytes: ArrayBuffer = await localDerivedBytesTask;
//         const remoteDerivedKey: string = await remoteDerivedKeyTask;

//         const localDerivedKey: string = arrayUtils.toCustomBase(localDerivedBytes, defaultAlphabet);

//         if (localDerivedKey !== remoteDerivedKey) {
//             throw new Error(`Keys mismatch at test ${i}, local: ${localDerivedKey}, remote: ${remoteDerivedKey}`);
//         }

//         // console.log(localDerivedKey);
//     }
// };

// export async function startValidation(): Promise<void> {
//     console.log('Validating...');
//     const start: number = window.performance.now();
//     await validate();
//     const duration: number = window.performance.now() - start;
//     console.log(`Validated (took ${duration / 1000} seconds)`);
// }
