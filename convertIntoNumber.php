
<?php
/**
     * Init function ConvertIntoNumber
     * @param $youtubeID
     * @return mixed|string
     * Converts youtubeID to simple ID
     */
    private function convertIntoNumber($youtubeID)
    {
        // Fetch the cached value
        $number = $this->memcached->get('to_number_' . $youtubeID);

        // Check it's empty
        if (empty($number)) {

            // Check youtube id it is exist in DB
            $number = $this->dbh->fetchOne("SELECT video_id FROM videos WHERE youtube_id = :youtube_id", array('youtube_id' => $youtubeID));

            // If it is empty
            if (empty($number)) {

                // Insert youtube Id
                if ($this->dbh->insert('videos', array('youtube_id' => $youtubeID))) {

                    // Get last insert Id
                    $number = $this->dbh->lastInsertId();

                    // Set the cache for this video
                    $this->memcached->set('to_number_' . $youtubeID, $number, 0);
                } else {
                    $number = '';
                }

                // Else set Cache
            } else {
                $this->memcached->set('to_number_' . $youtubeID, $number, 0);
                $number = '';
            }
        } else {
            $number = '';
        }

        // Return video_id
        return $number;
    }