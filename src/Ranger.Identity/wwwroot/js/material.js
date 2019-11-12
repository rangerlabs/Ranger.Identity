import * as mdc from '../lib/material-components-web/material-components-web.min.js';
// for accessing is as a window object
window.mdc = mdc;

// Or do this inside component
document.addEventListener('DOMContentLoaded', () => mdc.autoInit());
