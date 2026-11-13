const esc=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const unique=a=>[...new Set(a.filter(x=>x!==null&&x!==undefined&&x!==''))];
const opt=(values,old)=>values.map(x=>`<option value="${esc(x)}" ${x===old?'selected':''}>${esc(x)}</option>`).join('');
const templates={RF:{'整体法兰':23,'对焊法兰':27,'承插焊法兰':29},F:{'整体法兰':30,'对焊法兰':32,'承插焊法兰':33},M:{'整体法兰':34,'对焊法兰':27,'承插焊法兰':35},RJ:{'整体法兰':36,'对焊法兰':38,'承插焊法兰':40}};
let data,options,drawingMetadata,annotationBindings,counterbores;const tbody=document.querySelector('#inputs');
function faceKind(s){if(s.includes('/RF')||s.endsWith('/B'))return 'RF';if(s.endsWith('/F'))return 'F';if(s.endsWith('/M')||s.endsWith('/E'))return 'M';return 'RJ'}
function init(){tbody.innerHTML=['进口法兰/单个法兰','出口法兰'].map((x,i)=>`<tr data-index="${i}"><td>${x}</td><td><select class="standard"></select></td><td><select class="pressure"></select></td><td><select class="type"></select></td><td><select class="dn"></select></td><td><select class="face"></select></td><td><select class="insulation"><option>无</option><option>有</option></select></td></tr>`).join('');for(const [i,tr] of [...tbody.rows].entries()){tr.querySelector('.standard').innerHTML=opt(options.standards,'GB9124.1-19');refresh(tr,true);for(const [cls,wanted] of [['dn',i?'DN125':'DN150'],['face',i?'突面/RF':'凹面/F']]){const el=tr.querySelector('.'+cls);if([...el.options].some(o=>o.value===wanted))el.value=wanted}tr.addEventListener('change',e=>{if(e.target.classList.contains('standard')||e.target.classList.contains('pressure')||e.target.classList.contains('dn'))refresh(tr,false);if(tr.querySelector('.standard').value.includes('夹套'))tr.querySelector('.insulation').value='有';render()})}render()}
function refresh(tr,initial){let s=tr.querySelector('.standard').value;for(const [c,table] of [['pressure','pressure'],['type','types'],['dn','size'],['face','faces']]){const el=tr.querySelector('.'+c);let list=options[table][s]||[];if(c==='type')list=list.filter(x=>x==='整体法兰');if(c==='pressure')list=list.filter(p=>['突面法兰数据源','凹凸面法兰数据源','环连接面法兰数据源'].some(sheet=>data[sheet].rows.some(r=>r[1]===s&&r[2]===p)));if(c==='dn'){let pressure=tr.querySelector('.pressure').value;list=list.filter(dn=>['突面法兰数据源','凹凸面法兰数据源','环连接面法兰数据源'].some(sheet=>data[sheet].rows.some(r=>r[1]===s&&r[2]===pressure&&r[3]===dn)))}if(c==='face'){let pressure=tr.querySelector('.pressure').value,dn=tr.querySelector('.dn').value;list=list.filter(face=>{let kind=faceKind(face),sheet=kind==='RJ'?'环连接面法兰数据源':kind==='RF'?'突面法兰数据源':'凹凸面法兰数据源';return data[sheet].rows.some(r=>r[1]===s&&r[2]===pressure&&r[3]===dn)})}el.innerHTML=opt(list,initial?(c==='pressure'?'PN16':c==='type'?'整体法兰':''):el.value)}const ins=tr.querySelector('.insulation');if(s.includes('夹套'))ins.value='有';ins.disabled=s.includes('夹套')}
function render(){document.querySelector('#results').innerHTML=[...tbody.rows].map((tr,i)=>{let val=c=>tr.querySelector('.'+c).value;let kind=faceKind(val('face')),sheet=kind==='RJ'?'环连接面法兰数据源':kind==='RF'?'突面法兰数据源':'凹凸面法兰数据源';let ds=data[sheet],matches=ds.rows.filter(x=>x[1]===val('standard')&&x[2]===val('pressure')&&x[3]===val('dn'));let r=matches.length===1?matches[0]:null;let id=templates[kind]?.[val('type')];let warning=matches.length>1?'数据源中存在重复匹配，不能确定使用哪一行。':'该组合在对应数据源中未找到。';return `<div class="result"><h3>${i?'出口':'进口'} · ${esc(val('standard'))} / ${esc(val('pressure'))} / ${esc(val('dn'))} / IF / ${esc(val('face'))} / 保温：${esc(val('insulation'))}</h3>${id?`<div class="drawing"><div class="drawing-canvas"><img src="images/image${id}.png" alt="${esc(val('face'))} ${esc(val('type'))} 原工作簿图形参考"></div></div>`:''}${r?`<div class="fields">${ds.headers.map((h,j)=>{const allowed=[4,5,6,7,9,10,...(kind==='RF'?[11]:kind==='F'?[23,25,27]:kind==='M'?[24,26]:[23,24,25,26,27])];return h&&allowed.includes(j)&&r[j]!==null&&r[j]!==undefined&&String(r[j]).trim()!==''&&String(r[j]).trim()!=='—'?`<div class="field"><span>${esc(h)}</span><strong>${esc(r[j])}</strong></div>`:''}).join('')}</div>`:`<p>${warning}</p>`}</div>`}).join('')}
Promise.all(['/api/data','/api/options','/api/drawing-metadata','/api/annotation-bindings','/api/counterbores'].map(url=>fetch(url).then(r=>{if(!r.ok)throw Error('无法读取 '+url);return r.json()}))).then(([d,o,m,b,c])=>{data=d;options=o;drawingMetadata=m;annotationBindings=b;counterbores=c;init()}).catch(e=>{document.querySelector('#results').textContent='数据加载失败：'+e.message});


let renderVersion=0;const baseRender=render;render=function(){
 baseRender();const version=++renderVersion,button=document.querySelector('#print-button');let completed=0,failed=false;button.disabled=true;
 for(const [i,tr] of [...tbody.rows].entries()){
  const val=c=>tr.querySelector('.'+c).value;
  const params=new URLSearchParams({standard:val('standard'),pressure:val('pressure'),dn:val('dn'),face:val('face'),type:val('type'),insulation:val('insulation')});
  fetch('/api/lookup?'+params).then(async response=>({ok:response.ok,body:await response.json()})).then(async ({ok,body})=>{
   if(version!==renderVersion)return;const result=document.querySelectorAll('#results .result')[i];if(!result)return;
   if(ok){drawAnnotations(result,tr,body);await Promise.all([...result.querySelectorAll('.drawing-canvas img')].map(img=>img.decode()))}else{failed=true;const line=document.createElement('p');line.className='calc-error';line.textContent=body.error||'无法计算所选组合';result.insertBefore(line,result.querySelector('.fields'))}
  }).catch(()=>{if(version!==renderVersion)return;failed=true;const result=document.querySelectorAll('#results .result')[i];if(result){const line=document.createElement('p');line.className='calc-error';line.textContent='无法读取所选法兰数据，请重新选择';result.append(line)}}).finally(()=>{if(version===renderVersion&&++completed===tbody.rows.length)button.disabled=failed});
 }
};


function drawAnnotations(result,tr,body){
 const face=faceKind(tr.querySelector('.face').value),type=tr.querySelector('.type').value;
 const model=drawingMetadata[face+'|'+type],canvas=result.querySelector('.drawing-canvas');if(!model||!canvas)return;
 // Keep the overlay anchored even when the browser has cached an older stylesheet.
 Object.assign(canvas.style,{position:'relative',width:'100%',maxWidth:'650px',margin:'auto',containerType:'inline-size'});
 for(const d of model.details||[]){const c=d.crop,detail=document.createElement('span'),img=document.createElement('img');Object.assign(detail.style,{position:'absolute',overflow:'hidden',left:(d.x*100)+'%',top:(d.y*100)+'%',width:(d.width*100)+'%',height:(d.height*100)+'%'});Object.assign(img.style,{position:'absolute',maxWidth:'none',width:(100/(1-c.left-c.right))+'%',height:(100/(1-c.top-c.bottom))+'%',left:(-100*c.left/(1-c.left-c.right))+'%',top:(-100*c.top/(1-c.top-c.bottom))+'%'});img.src='images/'+d.image;img.alt='法兰局部放大图';detail.append(img);canvas.append(detail)}
 const r=body.row,fmt=(v,prefix='')=>v===null||v===undefined||v===''||v==='—'?null:prefix+v;
 // The tolerance sheet takes C and f1 from the RF data row, regardless of the drawing face.
 const rf=data['突面法兰数据源'].rows.filter(x=>x[1]===r[1]&&x[2]===r[2]&&x[3]===r[3]);
 const number=v=>v===null||v===undefined||v===''||!Number.isFinite(Number(v))?null:Number(v);
 const dn=number(String(r[3]).replace(/^DN/i,'')),c=rf.length===1?number(rf[0][8]):null,f1=rf.length===1?number(rf[0][11]):null,ring=face==='RJ'?number(r[23]):null;
 // Workbook: VLOOKUP(LEFT(thread,3), 锪平孔, 3, 0); tool correction is K minus that result.
 const thread=rf.length===1?String(rf[0][7]??'').slice(0,3):'',hole=number(counterbores[thread]),boltCircle=rf.length===1?number(rf[0][5]):null;
 const by=(n,steps)=>n===null?null:[...steps].reverse().find(([threshold])=>n>=threshold)?.[1]??null;
 const tolerance={
  '节距':String(r[1]).startsWith('HG')?'0.45~0.55':'0.50~0.56mm','锪平孔':hole===null?null:'⌀'+hole,'车刀矫正':hole===null||boltCircle===null?null:'⌀'+Number((boltCircle-hole).toFixed(6)),
  '法兰外径\nD':by(dn,[[0,'±1.5'],[401,'±3.0']]),'法兰螺栓孔中心圆直径\nK':'±0.8',
  '法兰厚度\nC':by(c,[[0,'+1.0 / 0'],[18.01,'+1.5 / 0'],[50.01,'+2.0 / 0']]),
  '法兰颈\n(对焊)\nN1':by(dn,[[0,'0 / -1.5'],[501,'0 / -8.0']]),
  '法兰颈\n(承插焊)\nN2':by(dn,[[0,'+1.0 / 0'],[51,'+2.0 / 0'],[151,'+3.0 / 0']]),
  '法兰孔径\nB,B1':by(dn,[[0,'+0.5 / 0'],[101,'+1.0 / 0'],[401,'+1.5 / 0'],[700,'+3.0 / 0']]),
  '承插深度\nT':'±0.25','法兰高度\n(对焊)\nH':by(dn,[[0,'±1.5'],[251,'±3.0']]),
  '法兰高度\n(承插焊)\nH1':by(dn,[[0,'+1.5 / -1.0'],[300,'+3.0 / -1.0']]),
  '焊端外径\nA':by(dn,[[0,'+2.0 / 0'],[126,'+4.0 / 0']]),'法兰密封面\nd':'±0.5',
  '密封面\nf1':by(f1,[[0,'0 / -0.5'],[2.1,'0 / -1.0']]),'密封面f2，f3':'+0.4 / 0',
  '凸面直径\nX':'0 / -0.5','凹面直径\nY':'+0.5 / 0','环连接槽\nR':by(ring,[[0,'+0.5 / 0'],[2.1,'±0.5']]),
  '环连接槽\nP':'±0.13','环连接槽深度\nE':'+0.4 / 0','环连接槽宽度\nF':'±0.2'};
 const labels={};for(const [cell,b] of Object.entries(annotationBindings[face+'|'+type]||{})){if(b.constant!==undefined)labels[cell]=b.constant;else if(b.tolerance){const v=tolerance[b.tolerance];if(v!=null)labels[cell]=v+(b.suffix||'')}else {const v=fmt(r[b.index]);if(v!==null)labels[cell]=(b.prefix||'')+v+(b.suffix||'')}};if(body.displayedThickness!==null)labels[type==='整体法兰'?'H31':'H30']=fmt(body.displayedThickness);
 if(body.displayedHeight!==null){const cell=face==='RF'?'O30':face==='M'?'M30':'N30';labels[cell]=fmt(body.displayedHeight)}
 if(type==='整体法兰'&&face==='RF'){labels.I31=fmt(r[10],'φ');labels.J29=fmt(r[11])}
 if(type==='整体法兰'&&face==='F'){labels.I31=fmt(r[10],'φ');labels.L31=fmt(r[27],'φ');labels.J29=fmt(r[23]);labels.K29=fmt(r[25])}
 if(type==='整体法兰'&&face==='M'){labels.J31=fmt(r[26],'φ');labels.I29=fmt(r[24])}
 if(type==='整体法兰'&&face==='RJ'){labels.I31=fmt(r[10],'φ');labels.K31=fmt(r[24],'φ');labels.J29=fmt(r[23]);labels.L29=fmt(r[25]);labels.M29=fmt(r[26]);labels.N29='23°±0.5°'}
 // A vertical tolerance belongs immediately after the value, not at a distant textbox position.
 const paired={},skip=new Set(),bindings=annotationBindings[face+'|'+type]||{};
 for(const a of model.annotations){if(a.rotation!==270||!bindings[a.cell]?.tolerance||labels[a.cell]==null)continue;const col=a.cell.match(/^[A-Z]+/)[0],row=Number(a.cell.slice(col.length));const base=model.annotations.filter(x=>x.cell.startsWith(col)&&/^[A-Z]+\d+$/.test(x.cell)&&x.cell.match(/^[A-Z]+/)[0]===col&&Number(x.cell.slice(col.length))<row&&x.rotation===a.rotation&&labels[x.cell]!=null&&!bindings[x.cell]?.tolerance).sort((x,y)=>Number(y.cell.slice(col.length))-Number(x.cell.slice(col.length)))[0];if(base){paired[base.cell]=labels[a.cell];skip.add(a.cell)}}
 for(const a of model.annotations){const value=labels[a.cell];if(value==null||skip.has(a.cell))continue;const el=document.createElement('span');el.className='drawing-label';Object.assign(el.style,{position:'absolute',display:'flex',alignItems:'center',justifyContent:a.align==='center'?'center':a.align==='left'?'flex-start':'flex-end',textAlign:a.align||'right',left:(a.x*100)+'%',top:(a.y*100)+'%',width:(a.width*100)+'%',height:(a.height*100)+'%',transform:`rotate(${a.rotation||0}deg)`,transformOrigin:'center',whiteSpace:'pre',lineHeight:'0.9',fontSize:a.fontScale?(a.fontScale*100)+'cqw':''});el.textContent=String(value).replace(/[φΦ∅Ø]/g,'⌀').replace(' / ','\n');if(paired[a.cell]!=null){const t=document.createElement('span');Object.assign(t.style,{whiteSpace:'pre',fontSize:'0.8em',lineHeight:'0.82',marginLeft:'0.12em',textAlign:'center'});t.textContent=String(paired[a.cell]).replace(' / ','\n');el.append(t)}canvas.append(el)}
}


document.querySelector('#print-button').addEventListener('click',()=>{const get=id=>document.getElementById(id).value||'—';document.querySelector('#print-meta').textContent=`合同号 ${get('contract')}  ·  泵型号 ${get('model')}  ·  位号 ${get('position')}  ·  零件 ${get('part')}`;window.print()});

