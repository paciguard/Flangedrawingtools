(() => {
  const button = document.querySelector('#exit-button');
  const status = document.querySelector('#exit-status');
  button.addEventListener('click', async () => {
    button.disabled = true;
    status.textContent = '';
    try {
      const response = await fetch('/api/quit', {
        method: 'POST',
        headers: {'Content-Type': 'application/json', 'X-Flange-Action': 'exit'},
        body: '{}'
      });
      if (!response.ok) throw new Error('无法退出，请使用系统托盘中的“退出”。');
      document.querySelector('main').innerHTML = '<section class="exit-result"><h1>工具已退出</h1><p>您可以关闭此浏览器标签页。</p></section>';
    } catch (error) {
      button.disabled = false;
      status.textContent = error.message || '退出失败，请重试。';
    }
  });
})();
