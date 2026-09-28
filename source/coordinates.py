"""Coordinate source conversion. Device developer service consumes WGS84."""
import math

def validate(lon, lat):
    lon, lat = float(lon), float(lat)
    if not math.isfinite(lon) or not math.isfinite(lat) or abs(lon)>180 or abs(lat)>90:
        raise ValueError('经度须为 -180～180，纬度须为 -90～90。')
    return lon, lat

def wgs_to_gcj(lon, lat):
    if not (73.66 < lon < 135.05 and 3.86 < lat < 53.55): return lon, lat
    x,y=lon-105,lat-35
    dlat=-100+2*x+3*y+0.2*y*y+0.1*x*y+0.2*math.sqrt(abs(x))
    dlat+=(20*math.sin(6*x*math.pi)+20*math.sin(2*x*math.pi))*2/3
    dlat+=(20*math.sin(y*math.pi)+40*math.sin(y*math.pi/3))*2/3
    dlat+=(160*math.sin(y*math.pi/12)+320*math.sin(y*math.pi/30))*2/3
    dlon=300+x+2*y+0.1*x*x+0.1*x*y+0.1*math.sqrt(abs(x))
    dlon+=(20*math.sin(6*x*math.pi)+20*math.sin(2*x*math.pi))*2/3
    dlon+=(20*math.sin(x*math.pi)+40*math.sin(x*math.pi/3))*2/3
    dlon+=(150*math.sin(x*math.pi/12)+300*math.sin(x*math.pi/30))*2/3
    rad=lat*math.pi/180
    magic=1-0.00669342162296594323*math.sin(rad)**2
    dlat=dlat*180/((6378245*(1-0.00669342162296594323))/(magic*math.sqrt(magic))*math.pi)
    dlon=dlon*180/(6378245/math.sqrt(magic)*math.cos(rad)*math.pi)
    return lon+dlon,lat+dlat

def to_wgs(lon,lat,source):
    lon,lat=validate(lon,lat)
    if source=='WGS84': return lon,lat
    if source=='BD09':
        x,y=lon-0.0065,lat-0.006
        z=math.hypot(x,y)-0.00002*math.sin(y*math.pi*3000/180)
        theta=math.atan2(y,x)-0.000003*math.cos(x*math.pi*3000/180)
        lon,lat=z*math.cos(theta),z*math.sin(theta)
    elif source!='GCJ02': raise ValueError('未知坐标来源。')
    if not (73.66 < lon < 135.05 and 3.86 < lat < 53.55): return lon,lat
    guess_lon,guess_lat=lon,lat
    for _ in range(8):
        calc_lon,calc_lat=wgs_to_gcj(guess_lon,guess_lat)
        guess_lon-=calc_lon-lon;guess_lat-=calc_lat-lat
    return validate(guess_lon,guess_lat)
