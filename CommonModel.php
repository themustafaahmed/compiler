<?php

class CommonModel {

	private $dbh;
	private $redis;
	private $siteSettings;


	public function __construct(){
        $this->dbh = Registry::get('dbh');
        $this->redis = Registry::get('redis');
        $this->siteSettings = Registry::get('siteSettings');
	}
	
	public function mergeFiltersNotes($notes){

		$one = isset($notes['excellent']) ? $notes['excellent'] : array();
		$two = isset($notes['good']) ? $notes['good'] : array();
		$three = isset($notes['bad']) ? $notes['bad'] : array();

		return array_merge($one, $two, $three);
	}

	public function calculateScore($rating, $factor, $weight){
		if($rating > $factor){
			$rating = $factor;
		}
		return ceil($this->scaleNum($rating, 0, $factor, 0, $weight));
	}

	public function calculateScoreTotal($score, $maxScore){
		return ceil($this->scaleNum($score, 0, $maxScore, 0, 100));
	}

	public function scaleNum($value = 0, $star1, $stop1, $start2, $stop2){
		if($value > $stop1){
			$value = $stop1;
		}

		if(is_string($value)){
			$value = 0;
		}

		return (($value - $start1) / ($stop1 - $start1)) * ($stop2 - $start2) + $start2;
	}

	public function isDeviceMobile(){

		// If we already check the device type
		if(!isset($_SESSION['isMobile'])){

			// Load tthe package
			Loader::loadCore('MobileDetect', 'package');

			// Init
			$mobileDetect = new MobileDetect();

			// Check if the device is mobile
			$isMobile = $mobileDetect->isMobile();

			// Create a session flag
			$_SESSION['isMobile'] = $isMobile;

			return $isMobile;

		} else {

			// Get the session flag
			return $isMobile = $_SESSION['isMobile'];

		}

	}

	public function calculateScoreTitle($score){
		if($score > 80){
			return 'EXCELENT';
		} else if ($score <= 80 & $score > 50){
			return 'GOOD';
		}  else if ($score <= 50 & $score > 30){
			return 'AVERAGE';
		} else {
			return 'BAD';
		}
	}

	public function getPopularComparisons(){

		$response = $this->redis->get('popularComparisons');
		if($response){
			$popularComparisons = unserialize($response);
		} else {
			$popularComparisons = $this->dbh->fetchAll("SELECT * FROM comparisons GROUP BY uniqueSlug ORDER BY views DESC LIMIT 5");
			$this->redis->set('popularComparisons', serialize($popularComparisons), 120);
		}

		return $popularComparisons;
		
	}


}

?>