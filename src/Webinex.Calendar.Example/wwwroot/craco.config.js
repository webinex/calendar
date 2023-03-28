const path = require('path');

module.exports = {
  webpack: {
    configure: function (webpackConfig) {
      return webpackConfig;
    },
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
};
