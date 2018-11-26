
<?php
/**
 * 
 */
class File
{
	
	 public function readFile($file_name)
    {
        $file = fopen("/var/wwwprefix/projects/repeat.eurocoders.com/crons/" . $file_name, "r") or die("Unable to open file!");
        $r_file = fread($file, filesize('/var/wwwprefix/projects/repeat.eurocoders.com/crons/'.$file_name));
        $read = $r_file;
        fclose($file);
        return $read;
    }

    public function writeFile($parameter, $file_name)
    {
        $file = fopen("/var/wwwprefix/projects/repeat.eurocoders.com/crons/" . $file_name, "w+") or die("Unable to open file!");
        fwrite($file, $parameter);
        fclose($file);
    }
}
$param = $getRelated->readFile("getVideos.txt");

$param = $param + 1000;
$param = $param . ",1000";
$getRelated->writeFile($param, "getVideos.txt");

?>