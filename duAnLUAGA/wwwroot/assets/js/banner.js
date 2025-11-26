
    (function () {
    const slider = document.querySelector('[data-hero-slider]');
    if (!slider) return;

    const viewport = slider.querySelector('[data-slider-viewport]');
    const track = slider.querySelector('[data-slider-track]');
    const slides = Array.from(slider.querySelectorAll('[data-slider-slide]'));
    const prevBtn = slider.querySelector('[data-slider-prev]');
    const nextBtn = slider.querySelector('[data-slider-next]');
    const dots = Array.from(slider.querySelectorAll('[data-slider-dot]'));

    let currentIndex = 0;
    let startPos = 0;
    let currentTranslate = 0;
    let prevTranslate = 0;
    let isDragging = false;
    let hasDragged = false;
    let animationId = 0;
    let autoplayId = null;
    const autoplayDelay = 5000; // 5s

    function getSliderWidth() {
      return viewport.clientWidth || viewport.offsetWidth || 1;
    }

    function setSliderPosition() {
        track.style.transform = `translateX(${currentTranslate}px)`;
    }

    function updateDots() {
        dots.forEach((dot, index) => {
            dot.classList.toggle('is-active', index === currentIndex);
        });
    }

    function goToSlide(index) {
      const total = slides.length;
    currentIndex = (index + total) % total;
    const width = getSliderWidth();
    currentTranslate = -currentIndex * width;
    prevTranslate = currentTranslate;
    setSliderPosition();
    updateDots();
    }

    function nextSlide() {
        goToSlide(currentIndex + 1);
    }

    function prevSlide() {
        goToSlide(currentIndex - 1);
    }

    function startAutoplay() {
      if (autoplayId) return;
    autoplayId = setInterval(nextSlide, autoplayDelay);
    }

    function stopAutoplay() {
      if (!autoplayId) return;
    clearInterval(autoplayId);
    autoplayId = null;
    }

    function restartAutoplay() {
        stopAutoplay();
    startAutoplay();
    }

    function animation() {
        setSliderPosition();
    if (isDragging) {
        animationId = requestAnimationFrame(animation);
      }
    }

    function pointerDown(event) {
        isDragging = true;
    hasDragged = false;
    startPos = event.clientX;
    slider.classList.add('is-dragging');
    stopAutoplay();
    animationId = requestAnimationFrame(animation);
    }

    function pointerMove(event) {
      if (!isDragging) return;
    const currentPosition = event.clientX;
    const diff = currentPosition - startPos;

      if (Math.abs(diff) > 5) {
        hasDragged = true;
      }

    currentTranslate = prevTranslate + diff;
    }

    function pointerUp() {
      if (!isDragging) return;
    isDragging = false;
    cancelAnimationFrame(animationId);
    slider.classList.remove('is-dragging');

    const width = getSliderWidth();
    const movedBy = currentTranslate - prevTranslate;

    if (movedBy < -width / 4) {
        nextSlide();
      } else if (movedBy > width / 4) {
        prevSlide();
      } else {
        goToSlide(currentIndex);
      }

    restartAutoplay();
    }

    // Nút điều hướng
    prevBtn.addEventListener('click', () => {
        prevSlide();
    restartAutoplay();
    });

    nextBtn.addEventListener('click', () => {
        nextSlide();
    restartAutoplay();
    });

    // Dots
    dots.forEach((dot, index) => {
        dot.addEventListener('click', () => {
            goToSlide(index);
            restartAutoplay();
        });
    });

    // Drag / swipe
    viewport.addEventListener('pointerdown', pointerDown);
    window.addEventListener('pointermove', pointerMove);
    window.addEventListener('pointerup', pointerUp);
    window.addEventListener('pointercancel', pointerUp);

    // Không cho click mở link khi đang kéo
    slides.forEach((slide) => {
        slide.addEventListener('click', (event) => {
            if (hasDragged) {
                event.preventDefault();
                event.stopImmediatePropagation();
            }
            hasDragged = false;
        });
    });

    // Hover dừng autoplay (desktop)
    slider.addEventListener('mouseenter', stopAutoplay);
    slider.addEventListener('mouseleave', startAutoplay);

    // Responsive: resize thì tính lại vị trí
    window.addEventListener('resize', () => {
        goToSlide(currentIndex);
    });

    // Init
    goToSlide(0);
    startAutoplay();
  })();


