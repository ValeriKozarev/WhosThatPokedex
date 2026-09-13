export function playAudio(element) {
    element.currentTime = 0;
    element.play();
}