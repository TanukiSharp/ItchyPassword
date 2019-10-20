const path = require('path');
const CopyPlugin = require('copy-webpack-plugin');

module.exports = {
    entry: './src/ui.ts',
    devtool: 'inline-source-map',
    module: {
        rules: [
            {
                use: 'ts-loader',
                exclude: /node_modules/,
            }
        ]
    },
    resolve: {
        extensions: ['.ts']
    },
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, '..', 'docs'),
    },
    plugins: [
        new CopyPlugin([
            { context: 'src', from: '*.html', to: '../docs' }
        ]),
    ]
};
