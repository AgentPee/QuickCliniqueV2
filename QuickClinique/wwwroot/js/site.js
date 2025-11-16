

// Navbar Scroll and Active Link Effects
document.addEventListener('DOMContentLoaded', function () {

    // ===================================
    // 1. SCROLL EFFECT FOR HEADER
    // ===================================
 

    // ===================================
    // 2. ACTIVE LINK HIGHLIGHTING
    // ===================================
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');

    // Loop through all navigation links
    navLinks.forEach(link => {
        // Get the href attribute
        const linkHref = link.getAttribute('href');

        // Skip if href is null or empty
        if (!linkHref) return;

        // Check if the link matches the current path
        // Also check if current path starts with the link (for child pages)
        if (linkHref === currentPath ||
            (linkHref !== '/' && currentPath.startsWith(linkHref + '/')) ||
            (linkHref !== '/' && currentPath.startsWith(linkHref))) {

            // Add active class to the link
            link.classList.add('active');

            // Also add active class to parent nav-item
            const parentLi = link.closest('.nav-item');
            if (parentLi) {
                parentLi.classList.add('active');
            }
        }
    });

    // ===================================
    // 3. DROPDOWN ITEM HIGHLIGHTING
    // ===================================
    const dropdownItems = document.querySelectorAll('.dropdown-item');

    dropdownItems.forEach(item => {
        const itemHref = item.getAttribute('href');

        // Skip if href is null or empty
        if (!itemHref) return;

        // Check if dropdown item matches current path
        if (itemHref === currentPath ||
            (itemHref !== '/' && currentPath.startsWith(itemHref + '/')) ||
            (itemHref !== '/' && currentPath.startsWith(itemHref))) {

            // Add active class to dropdown item
            item.classList.add('active');

            // Also highlight the parent dropdown toggle
            const dropdownToggle = item.closest('.nav-item')?.querySelector('.dropdown-toggle');
            if (dropdownToggle) {
                dropdownToggle.classList.add('active');
            }
        }
    });

    // ===================================
    // 4. SMOOTH SCROLL FOR ANCHOR LINKS (Optional)
    // ===================================
    // This adds smooth scrolling for hash links like #section
    const anchorLinks = document.querySelectorAll('a[href^="#"]');

    anchorLinks.forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');

            // Skip if it's just "#" or empty
            if (!targetId || targetId === '#') return;

            const targetElement = document.querySelector(targetId);

            if (targetElement) {
                e.preventDefault();

                // Scroll smoothly to the target
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });

                // Update URL without jumping
                if (history.pushState) {
                    history.pushState(null, null, targetId);
                }
            }
        });
    });

    // ===================================
    // 5. NAVBAR COLLAPSE ON MOBILE (Simple Dropdown)
    // ===================================
    // Auto-collapse navbar on mobile when clicking a link
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');

    if (navbarToggler && navbarCollapse) {
        // Close menu when clicking a nav link (on mobile)
        navLinks.forEach(link => {
            link.addEventListener('click', function () {
                // Check if navbar is expanded (on mobile)
                if (window.innerWidth <= 992 && navbarCollapse.classList.contains('show')) {
                    navbarToggler.click(); // Collapse the navbar
                }
            });
        });
    }

    // ===================================
    // 6. ADD SCROLL INDICATOR (Optional)
    // ===================================
    // Shows how far down the page you've scrolled
    function updateScrollIndicator() {
        const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
        const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
        const scrolled = (winScroll / height) * 100;

        // You can use this value to show a progress bar if you add one to your HTML
        // Example: document.getElementById('scrollIndicator').style.width = scrolled + '%';
    }

    // Uncomment to enable scroll indicator
    // window.addEventListener('scroll', updateScrollIndicator);

    // ===================================
    // 7. HIGHLIGHT SECTION ON SCROLL (Optional)
    // ===================================
    // This highlights nav links based on which section is visible
    // Useful for single-page applications with sections

    function highlightNavOnScroll() {
        const sections = document.querySelectorAll('section[id]');
        const scrollPosition = window.scrollY + 100; // Offset for header height

        sections.forEach(section => {
            const sectionTop = section.offsetTop;
            const sectionHeight = section.offsetHeight;
            const sectionId = section.getAttribute('id');

            if (scrollPosition >= sectionTop && scrollPosition < sectionTop + sectionHeight) {
                // Remove active from all links
                navLinks.forEach(link => {
                    link.classList.remove('active');
                    const parentLi = link.closest('.nav-item');
                    if (parentLi) parentLi.classList.remove('active');
                });

                // Add active to matching link
                const activeLink = document.querySelector(`.navbar-nav a[href="#${sectionId}"]`);
                if (activeLink) {
                    activeLink.classList.add('active');
                    const parentLi = activeLink.closest('.nav-item');
                    if (parentLi) parentLi.classList.add('active');
                }
            }
        });
    }

    // Uncomment to enable section-based highlighting
    // window.addEventListener('scroll', highlightNavOnScroll);

    // ===================================
    // 8. INITIAL SETUP
    // ===================================
    // Check scroll position on page load
    if (window.scrollY > 50) {
        document.querySelector('.header')?.classList.add('scrolled');
    }
});

// ===================================
// 9. EXPORT FOR MODULE USE (Optional)
// ===================================
// If you're using ES6 modules, you can export functions
// export { updateScrollIndicator, highlightNavOnScroll };