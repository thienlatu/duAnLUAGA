
    const emailInput = document.getElementById("email");
    const passInput = document.getElementById("pass");

    const errorEmail = document.getElementById("errorEmail");
    const errorPass = document.getElementById("errorPass");

    // Validate email khi đang nhập
    emailInput.addEventListener("input", function () {
        const value = emailInput.value.trim();

    if (value === "") {
        errorEmail.innerText = "Email không được để trống";
        }
    else if (!value.includes("@")) {
        errorEmail.innerText = "Email phải chứa @";
        }
    else {
        errorEmail.innerText = "";
        }
    });

    // Validate password khi đang nhập
    passInput.addEventListener("input", function () {
        const value = passInput.value.trim();

    if (value.length < 6) {
        errorPass.innerText = "Mật khẩu phải tối thiểu 6 ký tự";
        } else {
        errorPass.innerText = "";
        }
    });


