const target = process.env['API_PROXY_TARGET'] || 'http://localhost:5219';

module.exports = {
  '/api': {
    target,
    changeOrigin: true,
    secure: false,
  },
};
