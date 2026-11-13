// Render the current drawings locally, then let the user choose the destination.
(() => {
  const button = document.querySelector('#print-button');
  const status = document.querySelector('#export-status');
  const pad = n => String(n).padStart(2, '0');
  const safe = value => String(value || '').replace(/[\\/:*?"<>|\x00-\x1f]/g, '-').replace(/[. ]+$/g, '').slice(0, 40) || '未选择';
  function filename(date = new Date()) {
    const parts = [...document.querySelectorAll('#inputs tr')].map((row, i) => {
      const value = key => row.querySelector('.' + key)?.value || '';
      const face = value('face').split('/').pop();
      return [i === 0 ? '进口' : '出口', value('standard'), value('pressure'), value('dn'), face].map(safe).join('_');
    });
    const stamp = `${date.getFullYear()}${pad(date.getMonth() + 1)}${pad(date.getDate())}_${pad(date.getHours())}${pad(date.getMinutes())}${pad(date.getSeconds())}`;
    return `${parts.join('_')}_${stamp}.pdf`;
  }
  async function drawingPng(drawing) {
    const rect = drawing.getBoundingClientRect();
    if (!rect.width || !rect.height) throw new Error('绘图尚未加载');
    const details = [...drawing.querySelectorAll(':scope > span:not(.drawing-label)')];
    const labels = [...drawing.querySelectorAll(':scope > .drawing-label')];
    const boxes = [drawing, ...details, ...labels].map(item => item.getBoundingClientRect());
    const left = Math.min(...boxes.map(b => b.left)) - rect.left;
    const top = Math.min(...boxes.map(b => b.top)) - rect.top;
    const right = Math.max(...boxes.map(b => b.right)) - rect.left;
    const bottom = Math.max(...boxes.map(b => b.bottom)) - rect.top;
    const margin = 10, width = Math.ceil(right-left+2*margin), height = Math.ceil(bottom-top+2*margin);
    const canvas = document.createElement('canvas'), scale = 3;
    canvas.width = width*scale; canvas.height = height*scale;
    const ctx = canvas.getContext('2d');ctx.scale(scale,scale);ctx.fillStyle='#fff';ctx.fillRect(0,0,width,height);
    const x = v => v-rect.left-left+margin, y = v => v-rect.top-top+margin;
    function paintImage(img) { const b=img.getBoundingClientRect();ctx.drawImage(img,x(b.left),y(b.top),b.width,b.height); }
    const main=drawing.querySelector(':scope > img'); if (!main) throw new Error('未找到绘图图片'); paintImage(main);
    for(const detail of details){const b=detail.getBoundingClientRect(),img=detail.querySelector('img');if(!img)continue;ctx.save();ctx.beginPath();ctx.rect(x(b.left),y(b.top),b.width,b.height);ctx.clip();paintImage(img);ctx.restore()}
    for(const label of labels){
      const box=label.getBoundingClientRect(),style=getComputedStyle(label),w=label.offsetWidth,h=label.offsetHeight;
      const angle=Number((label.style.transform.match(/rotate\(([-.0-9]+)deg\)/)||[])[1]||0)*Math.PI/180;
      const text=[...label.childNodes].filter(n=>n.nodeType===Node.TEXT_NODE).map(n=>n.textContent).join('');
      const parts=[{text,style},...[...label.children].map(el=>({text:el.textContent,style:getComputedStyle(el)}))];
      function font(part){return `${part.style.fontWeight} ${part.style.fontSize} ${part.style.fontFamily}`}
      for(const part of parts){ctx.font=font(part);part.lines=part.text.split('\n');part.width=Math.max(0,...part.lines.map(line=>ctx.measureText(line).width));part.gap=parseFloat(part.style.marginLeft)||0}
      const total=parts.reduce((n,p)=>n+p.width+p.gap,0),align=style.justifyContent;
      let cursor=align==='center'?(w-total)/2:align==='flex-start'?0:w-total;
      ctx.save();ctx.translate(x(box.left+box.width/2),y(box.top+box.height/2));ctx.rotate(angle);ctx.translate(-w/2,-h/2);ctx.fillStyle=style.color;ctx.textBaseline='middle';
      for(const part of parts){cursor+=part.gap;ctx.font=font(part);const lineHeight=parseFloat(part.style.lineHeight)||parseFloat(part.style.fontSize);for(const [i,line] of part.lines.entries())ctx.fillText(line,cursor+(part.width-ctx.measureText(line).width)/2,h/2+(i-(part.lines.length-1)/2)*lineHeight);cursor+=part.width}
      ctx.restore();
    }
    const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/png'));if(!blob)throw new Error('无法生成绘图图片');
    return {bytes:await blob.arrayBuffer(),width,height};
  }
  async function makePdf(drawings) {
    await document.fonts.ready;
    const images = await Promise.all(drawings.map(drawingPng));
    const pdf = await PDFLib.PDFDocument.create();
    const page = pdf.addPage(PDFLib.PageSizes.A4);
    const {width:pageWidth, height:pageHeight} = page.getSize();
    const margin = 18, gap = 8, slotHeight = (pageHeight - 2 * margin - gap) / images.length;
    for (const [i, image] of images.entries()) {
      const embedded = await pdf.embedPng(image.bytes);
      const scale = Math.min((pageWidth - 2 * margin) / image.width, slotHeight / image.height);
      const w = image.width * scale, h = image.height * scale;
      page.drawImage(embedded, {x:(pageWidth-w)/2, y:pageHeight-margin-i*(slotHeight+gap)-(slotHeight+h)/2, width:w, height:h});
    }
    return pdf.save();
  }
  button.addEventListener('click', async () => {
    status.textContent = '';
    if (typeof window.showSaveFilePicker !== 'function') {
      status.textContent = '请选择 Chrome 或 Edge，并通过 localhost 或 HTTPS 打开工具，以选择 PDF 保存位置。';
      return;
    }
    const drawings = [...document.querySelectorAll('#results .drawing-canvas')];
    if (drawings.length !== 2) { status.textContent = '请先完成进口和出口法兰选择。'; return; }
    try {
      const handle = await window.showSaveFilePicker({suggestedName:filename(), types:[{description:'PDF 文件', accept:{'application/pdf':['.pdf']}}]});
      button.disabled = true; status.textContent = '正在生成 PDF…';
      const bytes = await makePdf(drawings);
      const writable = await handle.createWritable();
      try { await writable.write(bytes); await writable.close(); }
      catch (error) { await writable.abort(); throw error; }
      status.textContent = 'PDF 已保存。';
    } catch (error) {
      if (error.name !== 'AbortError') status.textContent = 'PDF 导出失败：' + (error.message || '未知错误');
    } finally { button.disabled = false; }
  });
})();
