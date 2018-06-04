<html>
<head><title>HealthKeeper - Login</title></head>
<body>

<h1>HealthKeeper - Session Page</h1>

<?php

session_start();

if(isset($_GET['username'])){
	$username = $_GET['username'];
	echo "<h2>Hello ".htmlspecialchars($username)."</h2> ";
}
else{
	echo "<h2>Hello</h2> ";
}

?>

Connect to Bluetooth Device:

<form>
<input type='submit' name='Pair Device' value='Pair Device' />
</form>

<?php

$device = "Pulse Oximeter";
echo "Connected Device : ".$device;

?>

<form id='data' method='post'>
<input type='text' name='data' id='data'  maxlength="500" />
</form>

<?php

if(isset($_POST['data'])){

	$file = fopen("data.txt", "w+");
	$content = $_POST['data'];
	echo "Data Values Stored : ";
	echo $content;
	fwrite($file, "Analyze = false".PHP_EOL);
	fwrite($file,$content.PHP_EOL);
	fclose($file);
	echo 	"<form id='analyze' method='post'>
	<input type='submit' name='analyze' value='analyze' />
	</form>";
}

?>



<?php

if(isset($_POST['analyze'])){

	$file = fopen("data.txt", "r");
	$content = "";
	while(!feof($file))
  	{
		$k= fgets($file);
		$pattern = "Analyze = false";
		if ( (preg_match("/Analyze = false/", $k)) )
 		{
 			$k = "Analyze = true".PHP_EOL;
 			$content = $content.$k;
 		}
		else
		{	
 		$content=$content.$k;
		}
  	}
  	fclose($file);
  	$file = fopen("data.txt", "w+");
	fwrite($file, $content.PHP_EOL);
	
	$i = 0;
	while(!preg_match("/Analyze = Done/", $k)){
		fclose($file);
		$file = fopen("data.txt", "r");
		$k = fgets($file);
		usleep(500000);
	}
	fclose($file);

	$file1 = fopen("result.txt", "r");
	$result = fgets($file1);
	$array = explode(",", $result);
	$count = (int)$array[0];
	$min = (int)$array[1];
	echo "For 1 hour of sleep data<br />";
	echo "AHI (Apnea-hypopnea index) = ".$count."<br />";
	echo "Minimum Oxygen Level reached : ".$min."<br />";
	$sev = "";
	if($count < 5){
		$sev = "None";
	}
	elseif($count < 15){
		$sev = "mild";
	}
	elseif($count < 30){
		$sev = "moderate";	
	}
	else{
		$sev = "Highly severe!";	
	}
	
	echo "Disease severity : ".$sev;	
	fclose($file1);
	
}

?>


</body>
</html>