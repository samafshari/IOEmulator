(function(){
  if (!window.webAudio) window.webAudio = {};
  var ctx = null;
  var masterGain = null;

  function ensureContext(){
    if (!ctx) {
      var AudioContext = window.AudioContext || window.webkitAudioContext;
      if (!AudioContext) return null;
      ctx = new AudioContext();
      masterGain = ctx.createGain();
      masterGain.gain.value = 0.1; // low volume default
      masterGain.connect(ctx.destination);
    }
    if (ctx.state === 'suspended') {
      ctx.resume();
    }
    return ctx;
  }

  window.webAudio.beep = function(){
    window.webAudio.playTone(800, 200);
  };

  window.webAudio.playTone = function(freq, dur){
    var c = ensureContext();
    if (!c) return;
    var osc = c.createOscillator();
    var gain = c.createGain();
    osc.type = 'square';
    osc.frequency.setValueAtTime(Math.max(37, Math.min(2000, freq|0)), c.currentTime);
    gain.gain.setValueAtTime(0.15, c.currentTime);
    osc.connect(gain);
    gain.connect(masterGain);
    osc.start();
    var tEnd = c.currentTime + Math.max(0, dur|0)/1000;
    osc.stop(tEnd);
  };

  window.webAudio.playMusicString = function(str){
    // Minimal placeholder: just a beep for now
    window.webAudio.beep();
  };
})();
