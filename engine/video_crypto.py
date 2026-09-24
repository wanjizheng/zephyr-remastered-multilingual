"""Authenticated local media envelope. Conceals loose files, not extraction-proof DRM."""
import hashlib
import os
from cryptography.hazmat.primitives.ciphers.aead import AESGCM
from video_key import KEY

MAGIC=b'ZEPHYR-VIDEO-1\x00'

def encrypt_video(raw, expected):
 if hashlib.sha256(raw).hexdigest()!=expected:raise ValueError('视频原文校验失败')
 nonce=os.urandom(12)
 return MAGIC+nonce+AESGCM(KEY).encrypt(nonce,raw,MAGIC+expected.encode('ascii'))

def decrypt_video(blob, expected):
 if not blob.startswith(MAGIC):raise ValueError('不支持的视频封装格式')
 try:
  offset=len(MAGIC);raw=AESGCM(KEY).decrypt(blob[offset:offset+12],blob[offset+12:],MAGIC+expected.encode('ascii'))
 except Exception as ex:raise ValueError('加密视频完整性校验失败') from ex
 if hashlib.sha256(raw).hexdigest()!=expected:raise ValueError('解密视频校验失败')
 return raw
