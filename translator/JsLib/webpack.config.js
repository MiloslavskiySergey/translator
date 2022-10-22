const path = require('path');

module.exports = {
	output: {
		path: path.resolve(__dirname, '../wwwroot/dist'),
		filename: '[name].bundle.js',
		library: 'TranslatorJs',
		globalObject: 'self',
		clean: true,
	},
	module: {
		rules: [
			{
				test: /\.js?$/,
				exclude: /node_modules/,
				use: {
					loader: "babel-loader"
				}
			},
			{
				test: /\.ts$/,
				use: 'ts-loader',
				exclude: /node_modules/,
			},
			{
				test: /\.css$/,
				use: ['style-loader', 'css-loader']
			},
			{
				test: /\.ttf$/,
				use: ['file-loader']
			}
		]
	},
	resolve: {
		extensions: ['.ts', '.js']
    }
};
