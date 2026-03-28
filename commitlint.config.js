export default {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'body-leading-blank': [2, 'always'],
    'body-empty': [2, 'never'],
    'body-max-line-length': [2, 'always', 100],
    'footer-leading-blank': [2, 'always'],
  },
};
