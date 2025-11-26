const overlay = document.getElementById("loginOverlay");
const panel = document.getElementById("loginPanel");
const openBtn = document.getElementById("openLogin");
const closeBtn = document.getElementById("closeLogin");

openBtn.onclick = () => {
    overlay.style.display = "block";
    panel.classList.add("active");
    document.body.style.overflow = "hidden";
};

closeBtn.onclick = closePopup;
overlay.onclick = closePopup;

function closePopup() {
    overlay.style.display = "none";
    panel.classList.remove("active");
    document.body.style.overflow = "auto";
}

// Đóng khi nhấn ESC
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && panel.classList.contains('active')) {
        closePopup();
    }
});