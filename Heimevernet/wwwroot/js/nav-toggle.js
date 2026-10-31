(function () {
    var toggle = document.getElementById('navToggle');
    var sidebar = document.getElementById('appSidebar');
    var backdrop = document.getElementById('sidebarBackdrop');
    if (!toggle || !sidebar || !backdrop) return;

    function openNav() {
        sidebar.classList.add('is-open');
        backdrop.classList.add('is-open');
        toggle.setAttribute('aria-expanded', 'true');
    }

    function closeNav() {
        sidebar.classList.remove('is-open');
        backdrop.classList.remove('is-open');
        toggle.setAttribute('aria-expanded', 'false');
    }

    toggle.addEventListener('click', function () {
        if (sidebar.classList.contains('is-open')) {
            closeNav();
        } else {
            openNav();
        }
    });

    backdrop.addEventListener('click', closeNav);

    sidebar.querySelectorAll('a').forEach(function (link) {
        link.addEventListener('click', closeNav);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeNav();
    });

    window.addEventListener('resize', function () {
        if (window.innerWidth > 900) closeNav();
    });
})();