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
var generic = new HashSet<string>{"S"+"DN20"+"PN16"+"RF"}; var keys = new HashSet<string>(generic){"J"+"DN20"+"PN16"+"RFIF"};
if (CombinationRules.GetWorkbookKey("S","PN16","DN20","RF","整体法兰",false,generic,keys,false)!="SDN20PN16RF") throw new Exception("generic key");
if (CombinationRules.GetWorkbookKey("J","PN16","DN20","RF","整体法兰",true,generic,keys,false)!=null) throw new Exception("missing insulation");
if (CombinationRules.GetWorkbookKey("J","PN16","DN20","RF","整体法兰",true,generic,keys,true)!="JDN20PN16RFIF") throw new Exception("typed IF key");
if (CombinationRules.GetWorkbookKey("J","PN16","DN20","RF","对焊法兰",true,generic,keys,true)!=null) throw new Exception("insulation WN");
Console.WriteLine("PASS workbook key checks");
