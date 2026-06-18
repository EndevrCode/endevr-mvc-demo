(function () {
    const anim = document.getElementById('startup-animation');

    // Only show on mobile
    const isMobile = window.innerWidth <= 768;

    const alreadyShown = sessionStorage.getItem('startupShown');

    if (isMobile && anim && !alreadyShown) {
        sessionStorage.setItem('startupShown', 'true');

        // Leave animation visible for 4 seconds
        setTimeout(() => {
            anim.remove();
        }, 4000);
    } else if (anim) {
        anim.remove();
    }
})();
