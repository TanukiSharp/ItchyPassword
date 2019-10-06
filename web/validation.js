const postData = async (url = '', data = {}) => {
    const response = await fetch(url, {
        method: 'POST',
        cache: 'no-cache',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    });

    return response.text();
};

const generateRandomString = (alphabet) => {
    const size = Math.random() * 8 + 24;

    let result = '';
    for (let i = 0; i < size; i += 1) {
        const index = Math.floor(Math.random() * alphabet.length);
        result += alphabet[index];
    }

    return result;
};

const validate = async () => {
    for (let i = 0; i < 150; i += 1) {
        const privateKey = generateRandomString(defaultCustomAlphabet);
        const publicKey = generateRandomString(defaultCustomAlphabet);

        const localDerivedBytesTask = generatePassword(privateKey, publicKey);

        const remoteDerivedKeyTask = postData('http://localhost:5000/', {
            privateKey,
            publicKey,
            iterations: 100000,
            algorithmName: "SHA512",
            alphabet: defaultCustomAlphabet
        });

        await Promise.all([localDerivedBytesTask, remoteDerivedKeyTask]);

        const localDerivedBytes = await localDerivedBytesTask;
        const remoteDerivedKey = await remoteDerivedKeyTask;

        const localDerivedKey = toCustomBase(localDerivedBytes, defaultCustomAlphabet);

        if (localDerivedKey !== remoteDerivedKey) {
            throw new Error(`Keys mismatch at test ${i}, local: ${localDerivedKey}, remote: ${remoteDerivedKey}`);
        }

        // console.log(localDerivedKey);
    }
};

const main = async () => {
    console.log('Validating...');
    const start = window.performance.now();
    await validate();
    const duration = window.performance.now() - start;
    console.log(`Validated (took ${duration / 1000} seconds)`);
}

main();
