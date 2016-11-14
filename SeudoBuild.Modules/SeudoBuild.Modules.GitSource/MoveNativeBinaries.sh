#!/bin/bash
SRC="NativeBinaries"
DST="../../NativeBinaries"
if test -d $DST; then rm -rf $DST; fi;
mv -f $SRC $DST
