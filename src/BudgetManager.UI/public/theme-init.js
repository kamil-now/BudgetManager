try {
  var t = localStorage.getItem('budget-manager.theme');
  if (t === 'light' || t === 'dark') document.documentElement.dataset.theme = t;
} catch (e) {}
