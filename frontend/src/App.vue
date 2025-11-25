<script setup lang="ts">
</script>

<template>
  <router-view />
</template>

<style>
html, body, #app {
  height: 100%;
  margin: 0;
  background-color: #202225; /* dark fallback to prevent white flash */
}

/* Prefer showing focus outlines for keyboard users only */
:focus {
  outline: none;
}

:focus-visible {
  outline: 2px solid rgba(114,137,218,0.6);
  outline-offset: 2px;
}
</style>
