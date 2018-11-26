<?php

// Set the lockFile path
$lockFile = dirname(__FILE__) . '/'.$lockFileName.'.lock';

// Creat handle, this also creates the file if it doesn't exist.
$lockFileHandle = fopen($lockFile, 'w') or die ('Cannot create lock file');

// True if the process is already running, false if not
$result = flock($lockFileHandle, LOCK_EX | LOCK_NB);
if ($result == false) {
    die("Already running, shutting down");
}