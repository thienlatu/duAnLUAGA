
const requirements = {
    length: { regex: /.{8,}/, element: document.getElementById('req-length') },
    upper: { regex: /[A-Z]/, element: document.getElementById('req-upper') },
    lower: { regex: /[a-z]/, element: document.getElementById('req-lower') },
    number: { regex: /[0-9]/, element: document.getElementById('req-number') },
    special: { regex: /[@$!%*?&]/, element: document.getElementById('req-special') }
};