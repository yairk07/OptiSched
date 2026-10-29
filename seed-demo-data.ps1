$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
function Heb([string]$s){ return [regex]::Unescape($s) }
$db = "C:\Users\yairk\source\repos\OptiSchedsa\shaldagaluf\App_Data\calnder.db1.accdb.mdb"
$cs = "Provider=Microsoft.ACE.OLEDB.16.0;Data Source=$db;Persist Security Info=False;"
function Get-Sha256([string]$text){ $sha=[System.Security.Cryptography.SHA256]::Create(); try{ return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace("-","").ToLower() } finally { $sha.Dispose() } }
function Add-Param($cmd,$value){
  if($value -is [int] -or $value -is [long]){ $p=$cmd.Parameters.Add("?",[System.Data.OleDb.OleDbType]::Integer); $p.Value=[int]$value }
  elseif($value -is [datetime]){ $p=$cmd.Parameters.Add("?",[System.Data.OleDb.OleDbType]::Date); $p.Value=$value }
  elseif($null -eq $value){ $p=$cmd.Parameters.Add("?",[System.Data.OleDb.OleDbType]::VarWChar); $p.Value=[DBNull]::Value }
  else { $p=$cmd.Parameters.Add("?",[System.Data.OleDb.OleDbType]::VarWChar,255); $p.Value=[string]$value }
}
function Exec-Sql($conn,$sql,[object[]]$values){ $cmd=$conn.CreateCommand(); $cmd.CommandText=$sql; foreach($v in $values){ Add-Param $cmd $v }; return $cmd.ExecuteNonQuery() }
function Get-Scalar($conn,$sql,[object[]]$values=@()){ $cmd=$conn.CreateCommand(); $cmd.CommandText=$sql; foreach($v in $values){ Add-Param $cmd $v }; return $cmd.ExecuteScalar() }
function Ensure-User($conn,$userName,$first,$last,$email,$password,$phone,$idNumber,$gender,$year,$city,$role){
  $hash=Get-Sha256 $password
  $existing=Get-Scalar $conn "SELECT [id] FROM [Users] WHERE [UserName]=?" @($userName)
  if($null -ne $existing -and "$existing" -ne ""){
    Exec-Sql $conn "UPDATE [Users] SET [FirstName]=?,[LastName]=?,[Email]=?,[password]=?,[PasswordHash]=?,[Gender]=?,[YearOfBirth]=?,[userid]=?,[UserIdNumber]=?,[phonenum]=?,[PhoneNumber]=?,[city]=?,[CityId]=?,[Role]=? WHERE [UserName]=?" @($first,$last,$email,$hash,$hash,$gender,$year,$idNumber,$idNumber,$phone,$phone,$city,$city,$role,$userName) | Out-Null
    Write-Output "UPDATED user=$userName id=$existing"; return [int]$existing
  }
  $maxObj=Get-Scalar $conn "SELECT MAX(Val([id])) FROM [Users]"; $nextId= if($null -eq $maxObj -or $maxObj -is [DBNull]){20}else{[int]$maxObj+1}
  while([int](Get-Scalar $conn "SELECT COUNT(*) FROM [Users] WHERE [id]=?" @("$nextId")) -gt 0){ $nextId++ }
  Exec-Sql $conn "INSERT INTO [Users] ([id],[UserName],[FirstName],[LastName],[Email],[password],[PasswordHash],[Gender],[YearOfBirth],[userid],[UserIdNumber],[phonenum],[PhoneNumber],[city],[CityId],[Role],[CreatedDate]) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)" @("$nextId",$userName,$first,$last,$email,$hash,$hash,$gender,$year,$idNumber,$idNumber,$phone,$phone,$city,$city,$role,(Get-Date)) | Out-Null
  Write-Output "CREATED user=$userName id=$nextId"; return $nextId
}
function Ensure-Member($conn,$calId,$userId,$role){
  $cnt=[int](Get-Scalar $conn "SELECT COUNT(*) FROM [SharedCalendarMembers] WHERE [CalendarId]=? AND [UserId]=?" @($calId,"$userId"))
  if($cnt -gt 0){ Exec-Sql $conn "UPDATE [SharedCalendarMembers] SET [Role]=? WHERE [CalendarId]=? AND [UserId]=?" @($role,$calId,"$userId") | Out-Null; Write-Output "MEMBER updated cal=$calId user=$userId role=$role" }
  else { Exec-Sql $conn "INSERT INTO [SharedCalendarMembers] ([CalendarId],[UserId],[Role],[JoinedDate]) VALUES (?,?,?,?)" @($calId,"$userId",$role,(Get-Date)) | Out-Null; Write-Output "MEMBER added cal=$calId user=$userId role=$role" }
}
function Add-SharedEvent($conn,$calId,$title,$date,$time,$notes,$category,$by){
  $cnt=[int](Get-Scalar $conn "SELECT COUNT(*) FROM [SharedCalendarEvents] WHERE [CalendarId]=? AND [Title]=?" @($calId,$title))
  if($cnt -gt 0){ Write-Output "EVENT skip cal=$calId title=$title"; return }
  Exec-Sql $conn "INSERT INTO [SharedCalendarEvents] ([CalendarId],[Title],[Date],[EventDate],[Time],[EventTime],[Notes],[category],[CreatedBy],[CreatedDate]) VALUES (?,?,?,?,?,?,?,?,?,?)" @($calId,$title,$date.Date,$date.Date,$time,$time,$notes,$category,"$by",(Get-Date)) | Out-Null
  Write-Output "EVENT shared cal=$calId title=$title"
}
function Add-PersonalEvent($conn,$userId,$title,$date,$time,$notes,$category){
  $cnt=[int](Get-Scalar $conn "SELECT COUNT(*) FROM [CalendarEvents] WHERE [UserId]=? AND [Title]=?" @($userId,$title))
  if($cnt -gt 0){ Write-Output "PERSONAL skip user=$userId title=$title"; return }
  Exec-Sql $conn "INSERT INTO [CalendarEvents] ([UserId],[Title],[EventDate],[EventTime],[Notes],[Category],[CreatedDate]) VALUES (?,?,?,?,?,?,?)" @($userId,$title,$date.Date,$time,$notes,$category,(Get-Date)) | Out-Null
  Write-Output "EVENT personal user=$userId title=$title"
}

$conn=New-Object System.Data.OleDb.OleDbConnection($cs); $conn.Open(); Write-Output "=== SEED START ==="
$pw="Demo123!"
$idYair=Ensure-User $conn "yair" (Heb "\u05D9\u05D0\u05D9\u05E8") (Heb "\u05E7\u05D3\u05D5\u05E9") "yairk07@gmail.com" $pw "0509507174" "333213023" "1" 2000 20 "owner"
$idNoa =Ensure-User $conn "noa_teacher" (Heb "\u05E0\u05D5\u05E2\u05D4") (Heb "\u05DC\u05D5\u05D9") "noa.teacher@optischedule.demo" $pw "0522345678" "234567899" "2" 1990 5 "user"
$idDani=Ensure-User $conn "dani_cohen" (Heb "\u05D3\u05E0\u05D9") (Heb "\u05DB\u05D4\u05DF") "dani.cohen@optischedule.demo" $pw "0533456789" "345678908" "1" 1992 8 "user"
$idMaya=Ensure-User $conn "maya_aviv" (Heb "\u05DE\u05D0\u05D9\u05D4") (Heb "\u05D0\u05D1\u05D9\u05D1") "maya.aviv@optischedule.demo" $pw "0506789012" "678901232" "2" 1999 1 "user"
$idRon =Ensure-User $conn "ron_scout" (Heb "\u05E8\u05D5\u05DF") (Heb "\u05E9\u05E4\u05D9\u05E8\u05D0") "ron.scout@optischedule.demo" $pw "0527890123" "789012341" "1" 1991 7 "user"
$idTal =Ensure-User $conn "tal_sport" (Heb "\u05D8\u05DC") (Heb "\u05DE\u05D6\u05E8\u05D7\u05D9") "tal.sport@optischedule.demo" $pw "0544567890" "456789015" "2" 1997 4 "user"
$idYael=Ensure-User $conn "yael_office" (Heb "\u05D9\u05E2\u05DC") (Heb "\u05E9\u05DE\u05E9") "yael.office@optischedule.demo" $pw "0555678901" "567890124" "2" 1994 2 "user"

foreach($u in @("roni","k07yair","demo_avi01","demo_noa02","ilai","itay")){
  $uid=Get-Scalar $conn "SELECT [id] FROM [Users] WHERE [UserName]=?" @($u)
  if($null -ne $uid -and "$uid" -ne ""){
    try{ Exec-Sql $conn "DELETE FROM [SharedCalendarMembers] WHERE [UserId]=?" @("$uid") | Out-Null }catch{}
    try{ Exec-Sql $conn "DELETE FROM [CalendarEvents] WHERE [UserId]=?" @([int]$uid) | Out-Null }catch{}
    Exec-Sql $conn "DELETE FROM [Users] WHERE [UserName]=?" @($u) | Out-Null
    Write-Output "DELETED junk user=$u"
  }
}

$nameSchool = Heb "\u05D1\u05D9\u05EA \u05E1\u05E4\u05E8"
$descSchool = Heb "\u05D9\u05D5\u05DE\u05DF \u05DE\u05E9\u05D5\u05EA\u05E3 \u05DC\u05E6\u05D5\u05D5\u05EA \u05D1\u05D9\u05EA \u05D4\u05E1\u05E4\u05E8"
$schoolIdObj=Get-Scalar $conn "SELECT [Id] FROM [SharedCalendars] WHERE [Name]=?" @($nameSchool)
if($null -eq $schoolIdObj){
  Exec-Sql $conn "INSERT INTO [SharedCalendars] ([Name],[Description],[CreatedBy],[CreatedDate]) VALUES (?,?,?,?)" @($nameSchool,$descSchool,"$idNoa",(Get-Date)) | Out-Null
  $schoolId=[int](Get-Scalar $conn "SELECT MAX([Id]) FROM [SharedCalendars]"); Write-Output "CREATED school id=$schoolId"
} else {
  $schoolId=[int]$schoolIdObj
  Exec-Sql $conn "UPDATE [SharedCalendars] SET [Description]=?,[CreatedBy]=?,[Name]=? WHERE [Id]=?" @($descSchool,"$idNoa",$nameSchool,$schoolId) | Out-Null
  Write-Output "UPDATED school id=$schoolId"
}
$cmd=$conn.CreateCommand(); $cmd.CommandText="SELECT [Id],[UserId] FROM [SharedCalendarMembers] WHERE [CalendarId]=$schoolId"; $r=$cmd.ExecuteReader(); $toDelete=@()
while($r.Read()){ $mu=[string]$r["UserId"]; if($mu -notin @("$idNoa","$idDani","$idMaya")){ $toDelete += [int]$r["Id"] } }; $r.Close()
foreach($mid in $toDelete){ Exec-Sql $conn "DELETE FROM [SharedCalendarMembers] WHERE [Id]=?" @($mid) | Out-Null; Write-Output "REMOVED extra school member=$mid" }
Ensure-Member $conn $schoolId $idNoa "admin"; Ensure-Member $conn $schoolId $idDani "member"; Ensure-Member $conn $schoolId $idMaya "member"

$nameScouts=Heb "\u05E6\u05D5\u05E4\u05D9\u05DD"; $descScouts=Heb "\u05DC\u05D5\u05D7 \u05E4\u05E2\u05D9\u05DC\u05D5\u05D9\u05D5\u05EA \u05E9\u05D1\u05D8 \u05D4\u05E6\u05D5\u05E4\u05D9\u05DD"
$scoutsIdObj=Get-Scalar $conn "SELECT [Id] FROM [SharedCalendars] WHERE [Name]=?" @($nameScouts)
if($null -eq $scoutsIdObj){ Exec-Sql $conn "INSERT INTO [SharedCalendars] ([Name],[Description],[CreatedBy],[CreatedDate]) VALUES (?,?,?,?)" @($nameScouts,$descScouts,"$idRon",(Get-Date)) | Out-Null; $scoutsId=[int](Get-Scalar $conn "SELECT MAX([Id]) FROM [SharedCalendars]") }
else { $scoutsId=[int]$scoutsIdObj; Exec-Sql $conn "UPDATE [SharedCalendars] SET [Description]=?,[CreatedBy]=? WHERE [Id]=?" @($descScouts,"$idRon",$scoutsId) | Out-Null }
Ensure-Member $conn $scoutsId $idRon "admin"; Ensure-Member $conn $scoutsId $idTal "member"; Ensure-Member $conn $scoutsId $idYair "member"

$nameMgmt=Heb "\u05E6\u05D5\u05D5\u05EA \u05D4\u05E0\u05D4\u05DC\u05D4"; $descMgmt=Heb "\u05E4\u05D2\u05D9\u05E9\u05D5\u05EA \u05D4\u05E0\u05D4\u05DC\u05D4 \u05D5\u05EA\u05DB\u05E0\u05D5\u05DF \u05E9\u05D1\u05D5\u05E2\u05D9"
$heatherId=Get-Scalar $conn "SELECT [Id] FROM [SharedCalendars] WHERE [Name]=?" @("heather")
if($null -ne $heatherId){ Exec-Sql $conn "UPDATE [SharedCalendars] SET [Name]=?,[Description]=?,[CreatedBy]=? WHERE [Id]=?" @($nameMgmt,$descMgmt,"$idYael",[int]$heatherId) | Out-Null; $mgmtId=[int]$heatherId }
else {
  $mgmtObj=Get-Scalar $conn "SELECT [Id] FROM [SharedCalendars] WHERE [Name]=?" @($nameMgmt)
  if($null -eq $mgmtObj){ Exec-Sql $conn "INSERT INTO [SharedCalendars] ([Name],[Description],[CreatedBy],[CreatedDate]) VALUES (?,?,?,?)" @($nameMgmt,$descMgmt,"$idYael",(Get-Date)) | Out-Null; $mgmtId=[int](Get-Scalar $conn "SELECT MAX([Id]) FROM [SharedCalendars]") }
  else { $mgmtId=[int]$mgmtObj }
}
Ensure-Member $conn $mgmtId $idYael "admin"; Ensure-Member $conn $mgmtId $idYair "member"; Ensure-Member $conn $mgmtId $idNoa "member"

$nameSport=Heb "\u05E1\u05E4\u05D5\u05E8\u05D8 \u05D1\u05D9\u05EA \u05E1\u05E4\u05E8\u05D9"; $descSport=Heb "\u05D0\u05D9\u05DE\u05D5\u05E0\u05D9\u05DD \u05D5\u05EA\u05D7\u05E8\u05D5\u05D9\u05D5\u05EA"
$alufId=Get-Scalar $conn "SELECT [Id] FROM [SharedCalendars] WHERE [Name]=?" @(Heb "\u05D0\u05DC\u05D5\u05E4")
if($null -ne $alufId){ Exec-Sql $conn "UPDATE [SharedCalendars] SET [Name]=?,[Description]=?,[CreatedBy]=? WHERE [Id]=?" @($nameSport,$descSport,"$idTal",[int]$alufId) | Out-Null; $sportId=[int]$alufId }
else {
  $sportObj=Get-Scalar $conn "SELECT [Id] FROM [SharedCalendars] WHERE [Name]=?" @($nameSport)
  if($null -eq $sportObj){ Exec-Sql $conn "INSERT INTO [SharedCalendars] ([Name],[Description],[CreatedBy],[CreatedDate]) VALUES (?,?,?,?)" @($nameSport,$descSport,"$idTal",(Get-Date)) | Out-Null; $sportId=[int](Get-Scalar $conn "SELECT MAX([Id]) FROM [SharedCalendars]") }
  else { $sportId=[int]$sportObj }
}
Ensure-Member $conn $sportId $idTal "admin"; Ensure-Member $conn $sportId $idDani "member"; Ensure-Member $conn $sportId $idRon "member"

try{ Exec-Sql $conn "DELETE FROM [SharedCalendarEvents] WHERE [Title]=?" @("ad") | Out-Null }catch{}
$today=(Get-Date).Date
$catMeet=Heb "\u05E4\u05D2\u05D9\u05E9\u05D4"; $catEvent=Heb "\u05D0\u05D9\u05E8\u05D5\u05E2"; $catTask=Heb "\u05DE\u05D8\u05DC\u05D4"; $catOther=Heb "\u05D0\u05D7\u05E8"

Add-SharedEvent $conn $schoolId (Heb "\u05D9\u05E9\u05D9\u05D1\u05EA \u05E6\u05D5\u05D5\u05EA \u05E9\u05D1\u05D5\u05E2\u05D9\u05EA") $today.AddDays(1) "14:00" (Heb "\u05D0\u05D5\u05DC\u05DD \u05DE\u05D5\u05E8\u05D9\u05DD") $catMeet $idNoa
Add-SharedEvent $conn $schoolId (Heb "\u05D8\u05E7\u05E1 \u05E4\u05EA\u05D9\u05D7\u05EA \u05E9\u05E0\u05D4") $today.AddDays(3) "09:00" (Heb "\u05D7\u05E6\u05E8 \u05D1\u05D9\u05EA \u05D4\u05E1\u05E4\u05E8") $catEvent $idNoa
Add-SharedEvent $conn $schoolId (Heb "\u05DE\u05D1\u05D7\u05DF \u05DE\u05EA\u05DE\u05D8\u05D9\u05E7\u05D4") $today.AddDays(5) "10:30" (Heb "\u05D7\u05D3\u05E8 12") $catTask $idDani
Add-SharedEvent $conn $schoolId (Heb "\u05D9\u05D5\u05DD \u05D4\u05D5\u05E8\u05D9\u05DD") $today.AddDays(10) "16:00" (Heb "\u05DB\u05D9\u05EA\u05D5\u05EA") $catMeet $idMaya
Add-SharedEvent $conn $schoolId (Heb "\u05D8\u05D9\u05D5\u05DC \u05E9\u05E0\u05EA\u05D9") $today.AddDays(18) "07:30" (Heb "\u05E9\u05E2\u05E8 \u05E8\u05D0\u05E9\u05D9") $catEvent $idNoa

Add-SharedEvent $conn $scoutsId (Heb "\u05DE\u05E1\u05D3\u05E8 \u05E9\u05D1\u05D5\u05E2\u05D9") $today.AddDays(2) "17:00" (Heb "\u05DE\u05D5\u05E2\u05D3\u05D5\u05DF") $catEvent $idRon
Add-SharedEvent $conn $scoutsId (Heb "\u05EA\u05E8\u05D2\u05D9\u05DC \u05E0\u05D9\u05D5\u05D5\u05D8") $today.AddDays(7) "08:00" (Heb "\u05D9\u05E2\u05E8") $catTask $idRon
Add-SharedEvent $conn $scoutsId (Heb "\u05DE\u05D3\u05D5\u05E8\u05D4") $today.AddDays(14) "19:30" (Heb "\u05E9\u05D8\u05D7 \u05DB\u05D9\u05E0\u05D5\u05E1") $catEvent $idTal

Add-SharedEvent $conn $mgmtId (Heb "\u05EA\u05DB\u05E0\u05D5\u05DF \u05D7\u05D5\u05D3\u05E9\u05D9") $today.AddDays(2) "11:00" (Heb "\u05D7\u05D3\u05E8 \u05D9\u05E9\u05D9\u05D1\u05D5\u05EA") $catMeet $idYael
Add-SharedEvent $conn $mgmtId (Heb "\u05E1\u05D9\u05DB\u05D5\u05DD \u05E8\u05D1\u05E2\u05D5\u05DF") $today.AddDays(21) "13:00" (Heb "\u05DE\u05E9\u05E8\u05D3") $catMeet $idYair

Add-SharedEvent $conn $sportId (Heb "\u05D0\u05D9\u05DE\u05D5\u05DF \u05DB\u05D3\u05D5\u05E8\u05E1\u05DC") $today.AddDays(1) "16:00" (Heb "\u05D0\u05D5\u05DC\u05DD \u05E1\u05E4\u05D5\u05E8\u05D8") $catEvent $idTal
Add-SharedEvent $conn $sportId (Heb "\u05DE\u05E9\u05D7\u05E7 \u05D9\u05D3\u05D9\u05D3\u05D5\u05EA") $today.AddDays(9) "18:00" (Heb "\u05DE\u05D2\u05E8\u05E9") $catEvent $idDani

Add-PersonalEvent $conn $idYair (Heb "\u05E1\u05E7\u05D9\u05E8\u05EA \u05DE\u05E2\u05E8\u05DB\u05EA OptiSched") $today "10:00" (Heb "\u05D4\u05DB\u05E0\u05D4 \u05DC\u05D4\u05E6\u05D2\u05D4") $catMeet
Add-PersonalEvent $conn $idYair (Heb "\u05E9\u05D9\u05D7\u05EA \u05E1\u05D8\u05D8\u05D5\u05E1 \u05E2\u05DD \u05E0\u05D5\u05E2\u05D4") $today.AddDays(2) "15:30" (Heb "\u05EA\u05D9\u05D0\u05D5\u05DD \u05D9\u05D5\u05DE\u05DF") $catMeet
Add-PersonalEvent $conn $idNoa (Heb "\u05D4\u05DB\u05E0\u05EA \u05DE\u05E2\u05E8\u05DA \u05E9\u05D9\u05E2\u05D5\u05E8") $today.AddDays(1) "20:00" (Heb "\u05E0\u05D5\u05E9\u05D0: \u05E9\u05D1\u05E8\u05D9\u05DD") $catTask
Add-PersonalEvent $conn $idDani (Heb "\u05D1\u05D3\u05D9\u05E7\u05EA \u05DE\u05D7\u05D1\u05E8\u05D5\u05EA") $today.AddDays(4) "19:00" (Heb "\u05DB\u05D9\u05EA\u05D4") $catTask
Add-PersonalEvent $conn $idMaya (Heb "\u05EA\u05D6\u05DB\u05D5\u05E8\u05EA \u05D9\u05D5\u05DD \u05D4\u05D5\u05E8\u05D9\u05DD") $today.AddDays(9) "08:00" (Heb "\u05D4\u05D5\u05D3\u05E2\u05D4 \u05DC\u05D4\u05D5\u05E8\u05D9\u05DD") $catOther
Add-PersonalEvent $conn $idRon (Heb "\u05E8\u05DB\u05E9 \u05E6\u05D9\u05D5\u05D3") $today.AddDays(5) "12:00" (Heb "\u05D7\u05E0\u05D5\u05EA \u05E1\u05E4\u05D5\u05E8\u05D8") $catTask
Add-PersonalEvent $conn $idTal (Heb "\u05D0\u05D9\u05DE\u05D5\u05DF \u05D0\u05D9\u05E9\u05D9") $today.AddDays(3) "07:00" (Heb "\u05E8\u05D9\u05E6\u05D4") $catEvent
Add-PersonalEvent $conn $idYael (Heb "\u05E1\u05D9\u05D3\u05D5\u05E8 \u05DC\u05D5\u05D7 \u05D6\u05DE\u05E0\u05D9\u05DD") $today.AddDays(1) "09:30" (Heb "\u05E2\u05D3\u05DB\u05D5\u05DF \u05D8\u05D1\u05DC\u05D4") $catMeet

Write-Output "=== SUMMARY ==="
$cmd=$conn.CreateCommand(); $cmd.CommandText="SELECT [id],[UserName],[FirstName],[LastName],[Role] FROM [Users] ORDER BY Val([id])"; $r=$cmd.ExecuteReader()
while($r.Read()){ Write-Output ("  id={0} {1} ({2} {3}) role={4}" -f $r["id"],$r["UserName"],$r["FirstName"],$r["LastName"],$r["Role"]) }; $r.Close()
Write-Output "Calendars+members:"
$cmd.CommandText="SELECT SC.[Id], SC.[Name], SC.[CreatedBy], M.[UserId], M.[Role] FROM [SharedCalendars] SC LEFT JOIN [SharedCalendarMembers] M ON SC.[Id]=M.[CalendarId] ORDER BY SC.[Id], M.[Role]"; $r=$cmd.ExecuteReader()
while($r.Read()){ Write-Output ("  cal={0} [{1}] by={2} member={3} role={4}" -f $r["Id"],$r["Name"],$r["CreatedBy"],$r["UserId"],$r["Role"]) }; $r.Close()
$cmd.CommandText="SELECT COUNT(*) FROM [SharedCalendarEvents]"; Write-Output ("SharedEvents="+$cmd.ExecuteScalar())
$cmd.CommandText="SELECT COUNT(*) FROM [CalendarEvents]"; Write-Output ("PersonalEvents="+$cmd.ExecuteScalar())
$cmd.CommandText="SELECT COUNT(*) FROM [SharedCalendarMembers] WHERE [CalendarId]=$schoolId"; Write-Output ("SchoolMembers="+$cmd.ExecuteScalar())
$conn.Close(); Write-Output "=== SEED DONE password=Demo123! ==="