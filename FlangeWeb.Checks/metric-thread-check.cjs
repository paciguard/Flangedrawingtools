const assert = require('assert');
const fs = require('fs');
const table = JSON.parse(fs.readFileSync('FlangeWeb/Data/metric-threads.json', 'utf8'));
const data = JSON.parse(fs.readFileSync('FlangeWeb/Data/flanges.json', 'utf8'));
assert.equal(table.standard, 'GB/T 196-2025');
// Basic dimensions, in mm, for the coarse threads used by the jacket data.
const expected = {
  M14:[14,2,12.701,11.835], M16:[16,2,14.701,13.835],
  M20:[20,2.5,18.376,17.294], M24:[24,3,22.051,20.752],
  M27:[27,3,25.051,23.752], M30:[30,3.5,27.727,26.211],
  M33:[33,3.5,30.727,29.211]
};
for (const [name, values] of Object.entries(expected)) {
  const p=table.threads[name]; assert(p, 'missing '+name);
  assert.deepStrictEqual([p.nominalDiameter,p.pitch,p.pitchDiameter,p.minorDiameter],values,name);
}
let count=0;
for (const sheet of Object.values(data)) for (const row of sheet.rows) {
  if (String(row[1]).includes('夹套')) {
    assert(table.threads[String(row[7]).trim()], 'unmapped jacket thread '+row[7]);
    count++;
  }
}
assert(count>0);console.log(`PASS GB/T 196-2025 basic metric dimensions and ${count} jacket rows`);
const rf=data['突面法兰数据源'].rows.filter(r=>String(r[1]).includes('夹套'));
const fm=data['凹凸面法兰数据源'].rows.filter(r=>String(r[1]).includes('夹套'));
for(const row of fm){
  const matches=rf.filter(x=>x[1]===row[1]&&x[2]===row[2]&&x[3]===row[3]);
  assert.equal(matches.length,1,'missing RF connection dimensions');
  for(const i of [4,5,6,7,9])assert.deepStrictEqual(row[i],matches[0][i],`shared connection dimension ${i}`);
}
const class300=fm.find(r=>r[1]==='HG20615-09-夹套'&&r[2]==='Class300'&&r[3]==='DN20');
const class300rf=rf.find(r=>r[1]===class300[1]&&r[2]===class300[2]&&r[3]===class300[3]);
assert.notEqual(class300[10],class300rf[10],'F/M-specific sealing-face diameter must be retained');
assert(!fm.some(r=>r[1]==='HG20615-09-夹套'&&r[2]==='Class150'),'Class150 F/M is not listed in the standard');
console.log(`PASS ${fm.length} jacket F/M shared RF connections and distinct sealing-face data`);
