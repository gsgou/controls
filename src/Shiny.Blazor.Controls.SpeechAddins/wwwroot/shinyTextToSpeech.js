// The browser's own synthesiser, the same way shinySpeechToText.js uses the recognizer. Shiny.Speech
// has a browser text-to-speech service too, but its interop module is not carried in the package at
// the version this repo pins - the app is expected to drop it into wwwroot - so the add-on owns this
// one rather than making every consumer copy a file.

let current = null;

export function isSupported() {
    return !!window.speechSynthesis;
}

export function getVoices() {
    if (!window.speechSynthesis) {
        return [];
    }
    return window.speechSynthesis.getVoices().map(v => v.name);
}

export function speak(dotnetRef, text, lang, voiceName, rate, pitch, volume) {
    if (!window.speechSynthesis) {
        dotnetRef.invokeMethodAsync('OnSpeechError', 'speechSynthesis not supported');
        return;
    }

    stop();

    const utterance = new SpeechSynthesisUtterance(text);
    if (lang) {
        utterance.lang = lang;
    }
    if (voiceName) {
        // getVoices() is empty until the voice list has loaded in some browsers; a miss just means the
        // default voice, which is a better outcome than refusing to speak.
        const voice = window.speechSynthesis.getVoices().find(v => v.name === voiceName);
        if (voice) {
            utterance.voice = voice;
        }
    }
    utterance.rate = rate;
    utterance.pitch = pitch;
    utterance.volume = volume;

    utterance.onend = () => {
        current = null;
        dotnetRef.invokeMethodAsync('OnSpeechEnd');
    };

    utterance.onerror = (event) => {
        current = null;
        // 'interrupted' and 'canceled' are what cancel() itself raises - the caller already knows.
        if (event.error === 'interrupted' || event.error === 'canceled') {
            dotnetRef.invokeMethodAsync('OnSpeechEnd');
        } else {
            dotnetRef.invokeMethodAsync('OnSpeechError', event.error ?? 'speech failed');
        }
    };

    current = utterance;
    window.speechSynthesis.speak(utterance);
}

export function stop() {
    if (current) {
        current = null;
    }
    window.speechSynthesis?.cancel();
}
