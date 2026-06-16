/* =========================================
   HEARTHLY — Main JavaScript
   ========================================= */

// ---- Theme Management ----
const THEME_KEY = 'hearthly-theme';
const FONT_KEY  = 'hearthly-fontsize';

function setTheme(theme) {
  localStorage.setItem(THEME_KEY, theme);
  document.documentElement.setAttribute('data-theme', theme);
  // Update Bootstrap theme
  document.documentElement.setAttribute('data-bs-theme',
    theme === 'system'
      ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
      : theme
  );
  document.querySelectorAll('.theme-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.theme === theme);
  });
}

function setFontSize(size) {
  localStorage.setItem(FONT_KEY, size);
  document.documentElement.setAttribute('data-fontsize', size);
  document.querySelectorAll('.font-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.size === size);
  });
}

function initTheme() {
  const theme = localStorage.getItem(THEME_KEY) || 'system';
  const size  = localStorage.getItem(FONT_KEY)  || 'medium';
  setTheme(theme);
  setFontSize(size);
}

// ---- Settings Drawer ----
function openSettingsDrawer() {
  closeMoreMenu();
  closeEmergency();
  document.getElementById('settingsDrawer')?.classList.add('open');
  document.getElementById('drawerOverlay')?.classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeSettingsDrawer() {
  document.getElementById('settingsDrawer')?.classList.remove('open');
  document.getElementById('drawerOverlay')?.classList.remove('open');
  document.body.style.overflow = '';
}

// ---- More Menu ----
let moreMenuOpen = false;

function toggleMoreMenu() {
  if (moreMenuOpen) { closeMoreMenu(); } else { openMoreMenu(); }
}

function openMoreMenu() {
  closeEmergency();
  closeSettingsDrawer();
  moreMenuOpen = true;
  document.getElementById('moreMenu')?.classList.add('open');
  document.getElementById('moreOverlay')?.classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeMoreMenu() {
  moreMenuOpen = false;
  document.getElementById('moreMenu')?.classList.remove('open');
  document.getElementById('moreOverlay')?.classList.remove('open');
  document.body.style.overflow = '';
}

// ---- Emergency Menu ----
let emergencyOpen = false;

function toggleEmergency() {
  if (emergencyOpen) { closeEmergency(); } else { openEmergency(); }
}

function openEmergency() {
  closeMoreMenu();
  closeSettingsDrawer();
  emergencyOpen = true;
  const btn = document.getElementById('emergencyBtn');
  btn?.classList.add('active');
  document.getElementById('emergencyMenu')?.classList.add('open');
  // Close on body click
  setTimeout(() => document.addEventListener('click', handleEmergencyOutside), 100);
}

function closeEmergency() {
  emergencyOpen = false;
  document.getElementById('emergencyBtn')?.classList.remove('active');
  document.getElementById('emergencyMenu')?.classList.remove('open');
  document.removeEventListener('click', handleEmergencyOutside);
}

function handleEmergencyOutside(e) {
  const menu = document.getElementById('emergencyMenu');
  const btn  = document.getElementById('emergencyBtn');
  if (!menu?.contains(e.target) && !btn?.contains(e.target)) {
    closeEmergency();
  }
}

function callEmergency(number) {
  closeEmergency();
  const clean = number.replace(/\s/g, '');
  if (confirm(`Call ${number}?`)) {
    window.location.href = `tel:${clean}`;
  }
}

// ---- Active Nav Tab ----
function setActiveNavTab() {
  const path = window.location.pathname.toLowerCase();
  document.querySelectorAll('.nav-tab').forEach(tab => {
    const href = tab.getAttribute('href')?.toLowerCase();
    if (href && path.startsWith(href) && href !== '/') {
      tab.classList.add('active');
    }
  });
}

// ---- PIN Keypad ----
let pinValue = '';
let pinTarget = null;

function initPinKeypad() {
  const keypad = document.getElementById('pinKeypad');
  if (!keypad) return;
  pinTarget = document.getElementById('pinInput');

  keypad.querySelectorAll('.pin-key').forEach(key => {
    key.addEventListener('click', () => {
      const val = key.dataset.value;
      if (val === 'del') {
        pinValue = pinValue.slice(0, -1);
      } else if (pinValue.length < 6) {
        pinValue += val;
      }
      updatePinDisplay();
    });
  });
}

function updatePinDisplay() {
  const dots = document.querySelectorAll('.pin-dot');
  dots.forEach((dot, i) => dot.classList.toggle('filled', i < pinValue.length));
  if (pinTarget) pinTarget.value = pinValue;
  // Auto-submit at 4-6 digits
  if (pinValue.length >= 4) {
    document.getElementById('pinForm')?.submit();
  }
}

// ---- Photo Preview ----
function initPhotoPreview() {
  document.querySelectorAll('input[type="file"][data-preview]').forEach(input => {
    input.addEventListener('change', function() {
      const preview = document.getElementById(this.dataset.preview);
      if (preview && this.files[0]) {
        const reader = new FileReader();
        reader.onload = e => { preview.src = e.target.result; };
        reader.readAsDataURL(this.files[0]);
      }
    });
  });
}

// ---- Shopping Item Toggle ----
function toggleShoppingItem(id, token) {
  fetch(`/Shopping/ToggleItem`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: `id=${id}&__RequestVerificationToken=${encodeURIComponent(token)}`
  }).then(() => {
    const circle = document.querySelector(`[data-item-id="${id}"] .check-circle`);
    const text   = document.querySelector(`[data-item-id="${id}"] .check-text`);
    circle?.classList.toggle('checked');
    text?.classList.toggle('checked-text');
  });
}

// ---- Password Visibility Toggle ----
function initPasswordToggle() {
  document.querySelectorAll('[data-password-toggle]').forEach(btn => {
    btn.addEventListener('click', function() {
      const target = document.getElementById(this.dataset.passwordToggle);
      if (!target) return;
      const isPassword = target.type === 'password';
      target.type = isPassword ? 'text' : 'password';
      this.querySelector('i')?.classList.toggle('fa-eye', !isPassword);
      this.querySelector('i')?.classList.toggle('fa-eye-slash', isPassword);
    });
  });
}

// ---- Service Worker Registration ----
function registerServiceWorker() {
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/js/sw.js')
      .then(reg => console.log('SW registered:', reg.scope))
      .catch(err => console.log('SW registration failed:', err));
  }
}

// ---- PWA Install Prompt ----
let deferredPrompt = null;

window.addEventListener('beforeinstallprompt', (e) => {
  e.preventDefault();
  deferredPrompt = e;
  const installBtn = document.getElementById('installBtn');
  if (installBtn) installBtn.style.display = 'flex';
});

function installPWA() {
  if (!deferredPrompt) return;
  deferredPrompt.prompt();
  deferredPrompt.userChoice.then(() => { deferredPrompt = null; });
}

// ---- System Theme Change ----
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
  if ((localStorage.getItem(THEME_KEY) || 'system') === 'system') setTheme('system');
});

// ---- Init ----
document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initPinKeypad();
  initPhotoPreview();
  initPasswordToggle();
  registerServiceWorker();

  // Close menus on escape
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
      closeMoreMenu();
      closeEmergency();
      closeSettingsDrawer();
    }
  });

  // Auto-hide toast
  const toast = document.getElementById('toastMsg');
  if (toast) setTimeout(() => toast.remove(), 3500);
});
