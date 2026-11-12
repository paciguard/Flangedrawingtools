using FlangeWeb;
void Check(string n, decimal? actual, decimal? expected) { if(actual!=expected) throw new Exception($"{n}: expected {expected}, got {actual}"); Console.WriteLine($"PASS {n}"); }
Check("PN RF IF", ThicknessRules.Calculate("HG20592-09","PN16","DN20","RF","整体法兰",20,18,2,2,4,null),18);
Check("Class RF IF", ThicknessRules.Calculate("HG20615-09","Class150","DN20","RF","整体法兰",20,18,2,null,null,null),20);
Check("PN F IF", ThicknessRules.Calculate("HG20592-09","PN16","DN20","F","整体法兰",20,18,null,2,4,null),16);
Check("Class F IF", ThicknessRules.Calculate("HG20615-09","Class300","DN20","F","整体法兰",20,18,null,null,null,null),18);
Check("PN M IF", ThicknessRules.Calculate("HG20592-09","PN16","DN20","M","整体法兰",20,18,null,null,4,null),14);
Check("PN RJ exception", ThicknessRules.Calculate("HG20592-09","PN40","DN125","RJ","整体法兰",20,18,null,null,null,3),18);
Check("PN RJ", ThicknessRules.Calculate("HG20592-09","PN40","DN150","RJ","整体法兰",20,18,null,null,null,3),15);
Check("missing not zero", ThicknessRules.Calculate("HG20592-09","PN16","DN20","F","整体法兰",20,18,null,null,null,null),null);
Check("unimplemented WN", ThicknessRules.Calculate("HG20592-09","PN16","DN20","F","对焊法兰",20,18,null,2,4,null),null);
