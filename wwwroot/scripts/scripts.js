window.scrollToBottom = () => {
    const scrollTarget = document.documentElement || document.body;

    const tryScroll = (attemptsLeft) => {
        if (scrollTarget.scrollHeight > 0) {
            scrollTarget.scrollTo({
                top: scrollTarget.scrollHeight,
                behavior: 'smooth'
            });
        } else if (attemptsLeft > 0) {
            setTimeout(() => tryScroll(attemptsLeft - 1), 50);
        }
    };

    tryScroll(5); // Retry up to 5 times
};