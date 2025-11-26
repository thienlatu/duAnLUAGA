
    (function () {
    const form = document.querySelector('#page-register .lv-form');
    if (!form) return;

    const full    = document.getElementById('lvFullname');
    const email   = document.getElementById('lvRegEmail');
    const pass    = document.getElementById('lvRegPassword');
    const confirm = document.getElementById('lvRegConfirm');

    const errFull  = document.getElementById('lvFullnameErr');
    const errEmail = document.getElementById('lvRegEmailErr');
    const errPass  = document.getElementById('lvRegPassErr');
    const errConf  = document.getElementById('lvRegConfirmErr');

    // Helper: set error cho field
    function setFieldError(input, errEl, message) {
      if (!input || !errEl) return;

    const hasError = Boolean(message && message.trim());
    errEl.textContent = message || '';
    input.setAttribute('aria-invalid', String(hasError));

    const wrapper = input.closest('.lv-field');
    if (wrapper) {
        wrapper.classList.toggle('is-error', hasError);
    wrapper.classList.toggle('is-valid', !hasError && input.value.trim() !== '');
      }
    }

    // =========================
    // Toggle show/hide password
    // =========================
    document.querySelectorAll('#page-register .lv-password__toggle').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.previousElementSibling;
            if (!input) return;

            const showing = input.type === 'text';
            input.type = showing ? 'password' : 'text';

            this.setAttribute('aria-pressed', String(!showing));

            const icon = this.querySelector('i');
            if (icon) {
                icon.classList.toggle('bi-eye');
                icon.classList.toggle('bi-eye-slash');
            }

            input.focus({ preventScroll: true });
        });
    });

    // =========================
    // Strength meter cho mật khẩu
    // (giữ logic gốc)
    // =========================
    const meter = document.querySelector('#page-register .lv-passmeter__bar');

    function scorePassword(pwd) {
        let score = 0;
    if (!pwd) return 0;
      if (pwd.length >= 8)  score++;
    if (/[A-Z]/.test(pwd)) score++;
    if (/\d/.test(pwd))    score++;
    if (/[^A-Za-z0-9]/.test(pwd)) score++;
      if (pwd.length >= 12) score++; // bonus
    return Math.min(score, 4); // 0..4
    }

    function renderMeter(val) {
      if (!meter) return;
    const widths = [0, 25, 50, 75, 100];
    meter.style.width = widths[val] + '%';
      // có thể đổi màu theo val nếu muốn
    }

    if (pass) {
        pass.addEventListener('input', () => renderMeter(scorePassword(pass.value)));
    }
    renderMeter(0);

    // =========================
    // Xác nhận mật khẩu
    // (bắt buộc nhập & phải khớp)
    // =========================
    function checkConfirm() {
      if (!confirm || !pass || !errConf) return true;

    const confVal = confirm.value;
    const passVal = pass.value;

    // Không nhập gì -> báo lỗi
    if (!confVal) {
        setFieldError(confirm, errConf, 'Vui lòng nhập lại mật khẩu để xác nhận.');
    return false;
      }

    // Có nhập nhưng không khớp
    if (passVal !== confVal) {
        setFieldError(confirm, errConf, 'Mật khẩu xác nhận không khớp.');
    return false;
      }

    // Ok
    setFieldError(confirm, errConf, '');
    return true;
    }

    if (confirm) {
        confirm.addEventListener('input', checkConfirm);
    }
    if (pass) {
        pass.addEventListener('input', checkConfirm);
    }

    // =========================
    // VALIDATE HỌ & TÊN
    // =========================
    function validateFullname(options) {
      if (!full || !errFull) return true;
    const opts = Object.assign({silentEmpty: false }, options);
    const value = full.value.trim();

    // Nếu bỏ trống
    if (!value) {
        if (!opts.silentEmpty) {
        setFieldError(full, errFull, 'Vui lòng nhập họ & tên.');
        } else {
        setFieldError(full, errFull, '');
        }
    return false;
      }

    // Ít nhất 2 từ
    const parts = value.split(/\s+/);
    if (parts.length < 2) {
        setFieldError(full, errFull, 'Vui lòng nhập đầy đủ họ và tên.');
    return false;
      }

    // Chỉ cho chữ + khoảng trắng + 1 số ký tự thường dùng
    if (!/^[A-Za-zÀ-ỹ\s'.-]+$/.test(value)) {
        setFieldError(full, errFull, 'Họ & tên chỉ được chứa chữ cái và khoảng trắng.');
    return false;
      }

    if (value.length < 4) {
        setFieldError(full, errFull, 'Họ & tên quá ngắn.');
    return false;
      }

    setFieldError(full, errFull, '');
    return true;
    }

    if (full) {
        full.addEventListener('blur', () => validateFullname({ silentEmpty: false }));
      full.addEventListener('input', () => validateFullname({silentEmpty: true }));
    }

    // =========================
    // VALIDATE EMAIL
    // =========================
    function validateEmail(options) {
      if (!email || !errEmail) return true;
    const opts = Object.assign({silentEmpty: false }, options);

    const value = email.value.trim();
    email.value = value; // trim luôn giá trị thực

    if (!value) {
        if (!opts.silentEmpty) {
        setFieldError(email, errEmail, 'Vui lòng nhập email.');
        } else {
        setFieldError(email, errEmail, '');
        }
    return false;
      }

    const basicPattern = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
    if (!basicPattern.test(value) || !email.checkValidity()) {
        setFieldError(email, errEmail, 'Email chưa hợp lệ.');
    return false;
      }

    setFieldError(email, errEmail, '');
    return true;
    }

    if (email) {
        email.addEventListener('blur', () => validateEmail({ silentEmpty: false }));
      email.addEventListener('input', () => validateEmail({silentEmpty: true }));
    }

    // =========================
    // Validate khi submit
    // =========================
    form.addEventListener('submit', function (e) {
      // reset error password trước (nếu có)
      if (pass && errPass) {
        setFieldError(pass, errPass, '');
      }

    let valid = true;

    // HTML5 validity cho toàn form (bắt các field khác nếu có)
    if (!form.checkValidity()) {
        valid = false;
      }

    // Custom validate fullname & email
    if (!validateFullname({silentEmpty: false })) valid = false;
    if (!validateEmail({silentEmpty: false }))    valid = false;

    // Mật khẩu
    if (pass && !pass.validity.valid) {
        setFieldError(pass, errPass, 'Mật khẩu tối thiểu 8 ký tự.');
    valid = false;
      }

    // Xác nhận mật khẩu (bắt buộc & khớp)
    if (!checkConfirm()) valid = false;

    if (!valid) {
        e.preventDefault();

    // Focus vào field đầu tiên bị lỗi
    const firstErrorInput =
    form.querySelector('.is-error input, .is-error textarea, .is-error select') ||
    form.querySelector('[aria-invalid="true"]');

    if (firstErrorInput && typeof firstErrorInput.focus === 'function') {
        firstErrorInput.focus();
        }
      }
    });

    // =========================
    // Enter để submit
    // =========================
    [full, email, pass, confirm].forEach(i => {
      if (!i) return;
      i.addEventListener('keydown', e => {
        if (e.key === 'Enter') {
        form.requestSubmit();
        }
      });
    });
  })();

