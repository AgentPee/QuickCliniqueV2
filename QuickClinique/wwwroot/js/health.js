// QuickClinic Health Pages JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // FAQ Accordion Functionality
    const healthQuestions = document.querySelectorAll('.health-question');

    healthQuestions.forEach(question => {
        question.addEventListener('click', function () {
            const answer = this.nextElementSibling;
            const isActive = answer.classList.contains('active');

            // Close all answers
            document.querySelectorAll('.health-answer').forEach(ans => {
                ans.classList.remove('active');
            });

            // Remove active class from all questions
            document.querySelectorAll('.health-question').forEach(q => {
                q.classList.remove('active');
            });

            // If clicked question wasn't active, open it
            if (!isActive) {
                answer.classList.add('active');
                this.classList.add('active');
            }
        });
    });

    // Search Functionality
    const searchInput = document.getElementById('healthSearch');
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCasen().trim();
            const healthItems = document.querySelectorAll('.health-item');
            let hasResults = false;

            healthItems.forEach(item => {
                const question = item.querySelector('.health-question h3').textContent.toLowerCase();
                const answer = item.querySelector('.health-answer').textContent.toLowerCase();
                const matches = question.includes(searchTerm) || answer.includes(searchTerm);

                if (matches) {
                    item.style.display = 'block';
                    hasResults = true;

                    // Highlight search term
                    if (searchTerm) {
                        highlightText(item, searchTerm);
                    } else {
                        removeHighlights(item);
                    }
                } else {
                    item.style.display = 'none';
                }
            });

            // Show no results message
            const noResults = document.getElementById('noResults') || createNoResultsMessage();
            if (!hasResults && searchTerm) {
                noResults.style.display = 'block';
            } else {
                noResults.style.display = 'none';
            }
        });
    }

    // Smooth scrolling for quick links
    const quickLinks = document.querySelectorAll('.link-card[href^="#"]');
    quickLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            const targetSection = document.querySelector(targetId);

            if (targetSection) {
                const offsetTop = targetSection.offsetTop - 100;
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Auto-open section if URL has hash
    if (window.location.hash) {
        const targetSection = document.querySelector(window.location.hash);
        if (targetSection) {
            setTimeout(() => {
                targetSection.scrollIntoView({ behavior: 'smooth' });
            }, 500);
        }
    }

    // Helper function to highlight text
    function highlightText(element, searchTerm) {
        const walker = document.createTreeWalker(
            element,
            NodeFilter.SHOW_TEXT,
            null,
            false
        );

        let node;
        const nodes = [];
        while (node = walker.nextNode()) {
            nodes.push(node);
        }

        nodes.forEach(node => {
            const parent = node.parentNode;
            if (parent.nodeName === 'SPAN' && parent.classList.contains('search-highlight')) {
                return; // Skip already highlighted nodes
            }

            const text = node.textContent;
            const regex = new RegExp(`(${escapeRegex(searchTerm)})`, 'gi');
            const newText = text.replace(regex, '<span class="search-highlight">$1</span>');

            if (newText !== text) {
                const newSpan = document.createElement('span');
                newSpan.innerHTML = newText;
                parent.replaceChild(newSpan, node);
            }
        });
    }

    // Helper function to remove highlights
    function removeHighlights(element) {
        const highlights = element.querySelectorAll('.search-highlight');
        highlights.forEach(highlight => {
            const parent = highlight.parentNode;
            parent.replaceChild(document.createTextNode(highlight.textContent), highlight);
            parent.normalize();
        });
    }

    // Helper function to escape regex characters
    function escapeRegex(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    // Helper function to create no results message
    function createNoResultsMessage() {
        const noResults = document.createElement('div');
        noResults.id = 'noResults';
        noResults.className = 'no-results';
        noResults.innerHTML = `
            <i class="fas fa-search"></i>
            <h3>No results found</h3>
            <p>Try different search terms or browse the categories above</p>
        `;
        noResults.style.display = 'none';
        document.querySelector('.health-sections').appendChild(noResults);
        return noResults;
    }

    // Add animation to cards on scroll
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    // Observe elements for animation
    const animatedElements = document.querySelectorAll('.link-card, .health-section');
    animatedElements.forEach(el => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(20px)';
        el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(el);
    });

    // Keyboard navigation for FAQ
    document.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            const activeElement = document.activeElement;
            const allQuestions = Array.from(document.querySelectorAll('.health-question'));
            const currentIndex = allQuestions.indexOf(activeElement);

            if (currentIndex !== -1) {
                let nextIndex;
                if (e.key === 'ArrowDown') {
                    nextIndex = (currentIndex + 1) % allQuestions.length;
                } else {
                    nextIndex = (currentIndex - 1 + allQuestions.length) % allQuestions.length;
                }

                allQuestions[nextIndex].focus();
            }
        } else if (e.key === 'Enter' && document.activeElement.classList.contains('health-question')) {
            document.activeElement.click();
        }
    });


});