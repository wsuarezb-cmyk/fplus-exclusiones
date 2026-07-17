window.lazyLoadImages = (imageSelector) => {
    const images = document.querySelectorAll(imageSelector);
    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.getAttribute('data-src');
                observer.unobserve(img);
            }
        });
    }, { threshold: 0.1 });

    images.forEach(img => observer.observe(img));
};
