const path = require('path');
const webpack = require('webpack');
const CopyPlugin = require('copy-webpack-plugin');
const GitRevisionPlugin = require('git-revision-webpack-plugin');

const gitRevisionPlugin = new GitRevisionPlugin();

module.exports = {
    entry: './src/index.ts',
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
        new webpack.DefinePlugin({
            'COMMITHASH': JSON.stringify(gitRevisionPlugin.commithash())
        })
    ]
};
