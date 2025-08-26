import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { LoadingButton } from '@/components/ui/loading-button';
import { Card, CardContent } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Link, RefreshCw, Copy, Check, Key, ExternalLink, AlertTriangle, Lightbulb } from 'lucide-react';
import { apiService, type ProxyConfig, type OAuthTokenInfo } from '@/services/api';
import { showToast } from '@/utils/toast';

interface OAuthFlowProps {
  platform: 'claude' | 'gemini' | 'thor' | 'openai';
  proxy?: ProxyConfig;
  onSuccess: (tokenInfo: OAuthTokenInfo) => void;
  onBack: () => void;
}

export default function OAuthFlow({ platform, proxy, onSuccess, onBack }: OAuthFlowProps) {
  const [loading, setLoading] = useState(false);
  const [exchanging, setExchanging] = useState(false);
  const [authUrl, setAuthUrl] = useState('');
  const [authCode, setAuthCode] = useState('');
  const [copied, setCopied] = useState(false);
  const [sessionId, setSessionId] = useState('');
  const [thorToken, setThorToken] = useState('');

  // 检查URL中的token参数（Thor平台回调）
  useEffect(() => {
    if (platform === 'thor') {
      const urlParams = new URLSearchParams(window.location.search);
      const token = urlParams.get('token');
      
      if (token) {
        setThorToken(token);
        showToast('成功获取Thor Token！', 'success');
        // 清理URL参数
        const url = new URL(window.location.href);
        url.searchParams.delete('token');
        url.searchParams.delete('platform');
        window.history.replaceState({}, document.title, url.pathname + url.hash);
      }
    }
  }, [platform]);

  const canExchange = platform === 'thor' ? thorToken.trim() : (authUrl && authCode.trim());

  // 监听授权码输入，自动提取URL中的code参数
  useEffect(() => {
    if (!authCode || typeof authCode !== 'string') return;
    
    const trimmedValue = authCode.trim();
    if (!trimmedValue) return;
    
    const isUrl = trimmedValue.startsWith('http://') || trimmedValue.startsWith('https://');
    
    if (isUrl) {
      if (trimmedValue.startsWith('http://localhost:45462')) {
        try {
          const url = new URL(trimmedValue);
          const code = url.searchParams.get('code');
          
          if (code) {
            setAuthCode(code);
            showToast('成功提取授权码！', 'success');
          } else {
            showToast('URL 中未找到授权码参数，请检查链接是否正确', 'error');
          }
        } catch (error) {
          showToast('链接格式错误，请检查是否为完整的 URL', 'error');
        }
      } else if (platform === 'gemini') {
        try {
          const url = new URL(trimmedValue);
          const code = url.searchParams.get('code');
          
          if (code) {
            setAuthCode(code);
            showToast('成功提取授权码！', 'success');
          }
        } catch (error) {
          // 不是有效的URL，保持原值
        }
      } else if (platform === 'thor') {
        try {
          const url = new URL(trimmedValue);
          const token = url.searchParams.get('token');
          
          if (token) {
            setThorToken(token);
            showToast('成功提取Thor Token！', 'success');
          } else {
            showToast('URL 中未找到token参数，请检查链接是否正确', 'error');
          }
        } catch (error) {
          showToast('链接格式错误，请检查是否为完整的 URL', 'error');
        }
      } else {
        showToast('请粘贴以 http://localhost:45462 开头的链接', 'error');
      }
    }
  }, [authCode, platform]);

  const generateAuthUrl = async () => {
    if (platform === 'thor') {
      // Thor平台直接打开授权页面
      const currentUrl = window.location.origin + window.location.pathname;
      const thorAuthUrl = `https://api.token-ai.cn/login?redirect_uri=${encodeURIComponent(currentUrl + '?platform=thor')}`;
      window.open(thorAuthUrl, '_blank');
      return;
    }
    
    setLoading(true);
    try {
      const proxyConfig = proxy?.enabled ? { proxy } : {};
      
      let result;
      if (platform === 'claude') {
        result = await apiService.generateClaudeAuthUrl(proxyConfig);
      } else if (platform === 'gemini') {
        result = await apiService.generateGeminiAuthUrl(proxyConfig);
      } else if (platform === 'openai') {
        result = await apiService.generateOpenAiAuthUrl(proxyConfig);
      }
      if (result) {
        setAuthUrl(result.authUrl);
        setSessionId(result.sessionId);
      }
    } catch (error: any) {
      showToast(error.message || '生成授权链接失败', 'error');
    } finally {
      setLoading(false);
    }
  };

  const regenerateAuthUrl = () => {
    setAuthUrl('');
    setAuthCode('');
    generateAuthUrl();
  };

  const copyAuthUrl = async () => {
    try {
      await navigator.clipboard.writeText(authUrl);
      setCopied(true);
      showToast('链接已复制', 'success');
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      const input = document.createElement('input');
      input.value = authUrl;
      document.body.appendChild(input);
      input.select();
      document.execCommand('copy');
      document.body.removeChild(input);
      setCopied(true);
      showToast('链接已复制', 'success');
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const exchangeCode = async () => {
    if (!canExchange) return;
    
    setExchanging(true);
    try {
      let data: any = {};
      
      if (platform === 'claude') {
        data = {
          sessionId: sessionId,
          callbackUrl: authCode.trim()
        };
      } else if (platform === 'gemini') {
        data = {
          code: authCode.trim(),
          sessionId: sessionId
        };
      } else if (platform === 'openai') {
        data = {
          code: authCode.trim(),
          sessionId: sessionId
        };
      } else if (platform === 'thor') {
        // Thor平台直接使用token
        const tokenInfo = {
          apiKey: thorToken.trim(),
          baseUrl: 'https://api.token-ai.cn/v1'
        };
        onSuccess(tokenInfo);
        return;
      }
      
      if (proxy?.enabled) {
        data.proxy = proxy;
      }
      
      let tokenInfo;
      if (platform === 'claude') {
        const result = await apiService.exchangeClaudeCode(data);
        tokenInfo = result.data.claudeAiOauth;
      } else if (platform === 'gemini') {
        tokenInfo = await apiService.exchangeGeminiCode(data);
      } else if (platform === 'openai') {
        const result = await apiService.exchangeOpenAiCode(data);
        tokenInfo = result.data.openAiOauth;
      }
      
      if (tokenInfo) {
        onSuccess(tokenInfo);
      }
    } catch (error: any) {
      showToast(error.message || '授权失败，请检查授权码是否正确', 'error');
    } finally {
      setExchanging(false);
    }
  };

  const renderClaudeFlow = () => (
    <Card className="border-border bg-card">
      <CardContent className="p-6">
        <div className="flex items-start gap-4">
          <div className="w-10 h-10 bg-primary rounded-xl flex items-center justify-center flex-shrink-0">
            <Link className="w-5 h-5 text-primary-foreground" />
          </div>
          <div className="flex-1">
            <h4 className="font-semibold text-foreground mb-3">
              Claude 账户授权
            </h4>
            <p className="text-sm text-muted-foreground mb-4">
              请按照以下步骤完成 Claude 账户的授权：
            </p>
            
            <div className="space-y-4">
              {/* 步骤1 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-primary text-primary-foreground rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      1
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        点击下方按钮生成授权链接
                      </p>
                      {!authUrl ? (
                        <LoadingButton
                          loading={loading}
                          onClick={generateAuthUrl}
                          size="sm"
                          icon={<Link className="w-4 h-4" />}
                          loadingText="生成中..."
                        >
                          生成授权链接
                        </LoadingButton>
                      ) : (
                        <div className="space-y-3">
                          <div className="flex items-center gap-2">
                            <Input 
                              value={authUrl}
                              readOnly
                              className="text-xs font-mono bg-muted flex-1"
                            />
                            <Button 
                              size="sm"
                              variant="outline"
                              onClick={copyAuthUrl}
                            >
                              {copied ? <Check className="w-4 h-4 text-primary" /> : <Copy className="w-4 h-4" />}
                            </Button>
                            <Button 
                              size="sm"
                              onClick={() => window.open(authUrl, '_blank')}
                              className="bg-primary hover:bg-primary/90 text-primary-foreground"
                            >
                              <ExternalLink className="w-4 h-4" />
                            </Button>
                          </div>
                          <Button 
                            variant="ghost"
                            size="sm"
                            onClick={regenerateAuthUrl}
                          >
                            <span className="flex items-center">
                              <RefreshCw className="w-3 h-3 mr-1" />
                              重新生成
                            </span>
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤2 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-primary text-primary-foreground rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      2
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        在浏览器中打开链接并完成授权
                      </p>
                      <p className="text-sm text-muted-foreground mb-2">
                        请在新标签页中打开授权链接，登录您的 Claude 账户并授权。
                      </p>
                      <div className="bg-muted p-3 rounded-xl border border-border">
                        <p className="text-xs text-muted-foreground flex items-start gap-2">
                          <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                          <span>
                            <strong>注意：</strong>如果您设置了代理，请确保浏览器也使用相同的代理访问授权页面。
                          </span>
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤3 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-primary text-primary-foreground rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      3
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        输入 Authorization Code
                      </p>
                      <p className="text-sm text-muted-foreground mb-3">
                        授权完成后，页面会显示一个 <strong>Authorization Code</strong>，请将其复制并粘贴到下方输入框：
                      </p>
                      <div className="space-y-3">
                        <div>
                          <Label className="flex items-center gap-2 text-sm font-semibold text-foreground mb-2">
                            <Key className="w-4 h-4 text-primary" />
                            Authorization Code
                          </Label>
                          <textarea 
                            value={authCode}
                            onChange={(e) => setAuthCode(e.target.value)}
                            rows={3}
                            className="w-full p-2 border border-input rounded-xl resize-none font-mono text-sm bg-background text-foreground"
                            placeholder="粘贴从Claude页面获取的Authorization Code..."
                          />
                        </div>
                        <p className="text-xs text-muted-foreground flex items-center gap-1">
                          <ExternalLink className="w-3 h-3" />
                          请粘贴从Claude页面复制的Authorization Code
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderGeminiFlow = () => (
    <Card className="border-border bg-card">
      <CardContent className="p-6">
        <div className="flex items-start gap-4">
          <div className="w-10 h-10 bg-primary rounded-xl flex items-center justify-center flex-shrink-0">
            <ExternalLink className="w-5 h-5 text-primary-foreground" />
          </div>
          <div className="flex-1">
            <h4 className="font-semibold text-foreground mb-3">
              Gemini 账户授权
            </h4>
            <p className="text-sm text-muted-foreground mb-4">
              请按照以下步骤完成 Gemini 账户的授权：
            </p>
            
            <div className="space-y-4">
              {/* 步骤1 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-primary text-primary-foreground rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      1
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        点击下方按钮生成授权链接
                      </p>
                      {!authUrl ? (
                        <LoadingButton
                          loading={loading}
                          onClick={generateAuthUrl}
                          size="sm"
                          icon={<Link className="w-4 h-4" />}
                          loadingText="生成中..."
                        >
                          生成授权链接
                        </LoadingButton>
                      ) : (
                        <div className="space-y-3">
                          <div className="flex items-center gap-2">
                            <Input 
                              value={authUrl}
                              readOnly
                              className="text-xs font-mono bg-muted flex-1"
                            />
                            <Button 
                              size="sm"
                              variant="outline"
                              onClick={copyAuthUrl}
                            >
                              {copied ? <Check className="w-4 h-4 text-primary" /> : <Copy className="w-4 h-4" />}
                            </Button>
                            <Button 
                              size="sm"
                              onClick={() => window.open(authUrl, '_blank')}
                              className="bg-primary hover:bg-primary/90 text-primary-foreground"
                            >
                              <ExternalLink className="w-4 h-4" />
                            </Button>
                          </div>
                          <Button 
                            variant="ghost"
                            size="sm"
                            onClick={regenerateAuthUrl}
                          >
                            <span className="flex items-center">
                              <RefreshCw className="w-3 h-3 mr-1" />
                              重新生成
                            </span>
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤2 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-primary text-primary-foreground rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      2
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        在浏览器中打开链接并完成授权
                      </p>
                      <ol className="text-sm text-muted-foreground space-y-1 list-decimal list-inside mb-3">
                        <li>点击上方的授权链接，在新页面中完成Google账号登录</li>
                        <li>点击"登录"按钮后可能会加载很慢（这是正常的）</li>
                        <li>如果超过1分钟还在加载，请按 F5 刷新页面</li>
                        <li>授权完成后会跳转到 http://localhost:45462 (可能显示无法访问)</li>
                      </ol>
                      <div className="bg-muted p-3 rounded-xl border border-border">
                        <p className="text-xs text-muted-foreground flex items-start gap-2">
                          <Lightbulb className="w-4 h-4 mt-0.5 flex-shrink-0" />
                          <span>
                            <strong>提示：</strong>如果页面一直无法跳转，可以打开浏览器开发者工具（F12），F5刷新一下授权页再点击页面的登录按钮，在"网络"标签中找到以 localhost:45462 开头的请求，复制其完整URL。
                          </span>
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤3 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-primary text-primary-foreground rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      3
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        复制oauth后的链接
                      </p>
                      <p className="text-sm text-muted-foreground mb-3">
                        复制浏览器地址栏的完整链接并粘贴到下方输入框：
                      </p>
                      <div className="space-y-3">
                        <div>
                          <Label className="flex items-center gap-2 text-sm font-semibold text-foreground mb-2">
                            <Key className="w-4 h-4 text-primary" />
                            复制oauth后的链接
                          </Label>
                          <textarea 
                            value={authCode}
                            onChange={(e) => setAuthCode(e.target.value)}
                            rows={3}
                            className="w-full p-2 border border-input rounded-xl resize-none font-mono text-sm bg-background text-foreground"
                            placeholder="粘贴以 http://localhost:45462 开头的完整链接..."
                          />
                        </div>
                        <div className="space-y-1">
                          <p className="text-xs text-muted-foreground flex items-center gap-1">
                            <Check className="w-3 h-3 text-primary" />
                            支持粘贴完整链接，系统会自动提取授权码
                          </p>
                          <p className="text-xs text-muted-foreground flex items-center gap-1">
                            <Check className="w-3 h-3 text-primary" />
                            也可以直接粘贴授权码（code参数的值）
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderThorFlow = () => (
    <Card className="border-border bg-card">
      <CardContent className="p-6">
        <div className="flex items-start gap-4">
          <div className="w-10 h-10 bg-blue-500 rounded-xl flex items-center justify-center flex-shrink-0">
            <ExternalLink className="w-5 h-5 text-white" />
          </div>
          <div className="flex-1">
            <h4 className="font-semibold text-foreground mb-3">
              Thor 平台授权
            </h4>
            <p className="text-sm text-muted-foreground mb-4">
              请按照以下步骤完成 Thor 平台的授权：
            </p>
            
            <div className="space-y-4">
              {/* 步骤1 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-blue-500 text-white rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      1
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        点击下方按钮跳转到授权页面
                      </p>
                      <LoadingButton
                        loading={loading}
                        onClick={generateAuthUrl}
                        size="sm"
                        icon={<ExternalLink className="w-4 h-4" />}
                        loadingText="跳转中..."
                        className="bg-blue-500 hover:bg-blue-600 text-white"
                      >
                        获取授权
                      </LoadingButton>
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤2 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-blue-500 text-white rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      2
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        在授权页面完成登录
                      </p>
                      <p className="text-sm text-muted-foreground mb-3">
                        在新打开的授权页面中完成登录，授权成功后页面会自动跳转回来并在URL中包含token参数。
                      </p>
                      <div className="bg-blue-50 dark:bg-blue-950 p-3 rounded-xl border border-blue-200 dark:border-blue-800">
                        <p className="text-xs text-blue-700 dark:text-blue-300 flex items-start gap-2">
                          <Lightbulb className="w-4 h-4 mt-0.5 flex-shrink-0" />
                          <span>
                            <strong>说明：</strong>Thor平台使用OpenAI兼容格式，支持自动获取Token的快捷访问。
                          </span>
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤3 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-blue-500 text-white rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      3
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        复制包含token的链接
                      </p>
                      <p className="text-sm text-muted-foreground mb-3">
                        授权完成后，复制浏览器地址栏中包含token参数的完整链接并粘贴到下方（支持 ?token=xxx 或 ?a=b&token=xxx 等格式）：
                      </p>
                      <div className="space-y-3">
                        <div>
                          <Label className="flex items-center gap-2 text-sm font-semibold text-foreground mb-2">
                            <Key className="w-4 h-4 text-blue-500" />
                            包含Token的链接
                          </Label>
                          <textarea 
                            value={authCode}
                            onChange={(e) => setAuthCode(e.target.value)}
                            rows={3}
                            className="w-full p-2 border border-input rounded-xl resize-none font-mono text-sm bg-background text-foreground"
                            placeholder="粘贴包含token参数的完整链接（如：https://example.com?token=xxx 或 https://example.com?a=b&token=xxx）..."
                          />
                        </div>
                        {thorToken && (
                          <div className="p-3 bg-green-50 dark:bg-green-950 border border-green-200 dark:border-green-800 rounded-xl">
                            <p className="text-xs text-green-700 dark:text-green-300 flex items-center gap-2">
                              <Check className="w-4 h-4" />
                              已成功提取Token: {thorToken.substring(0, 20)}...
                            </p>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderOpenAiFlow = () => (
    <Card className="border-border bg-card">
      <CardContent className="p-6">
        <div className="flex items-start gap-4">
          <div className="w-10 h-10 bg-slate-500 rounded-xl flex items-center justify-center flex-shrink-0">
            <Link className="w-5 h-5 text-white" />
          </div>
          <div className="flex-1">
            <h4 className="font-semibold text-foreground mb-3">
              OpenAI 账户授权
            </h4>
            <p className="text-sm text-muted-foreground mb-4">
              请按照以下步骤完成 OpenAI 账户的授权：
            </p>
            
            <div className="space-y-4">
              {/* 步骤1 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-slate-500 text-white rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      1
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        点击下方按钮生成授权链接
                      </p>
                      {!authUrl ? (
                        <LoadingButton
                          loading={loading}
                          onClick={generateAuthUrl}
                          size="sm"
                          icon={<Link className="w-4 h-4" />}
                          loadingText="生成中..."
                        >
                          生成授权链接
                        </LoadingButton>
                      ) : (
                        <div className="space-y-3">
                          <div className="flex items-center gap-2">
                            <Input 
                              value={authUrl}
                              readOnly
                              className="text-xs font-mono bg-muted flex-1"
                            />
                            <Button 
                              size="sm"
                              variant="outline"
                              onClick={copyAuthUrl}
                            >
                              {copied ? <Check className="w-4 h-4 text-primary" /> : <Copy className="w-4 h-4" />}
                            </Button>
                            <Button 
                              size="sm"
                              onClick={() => window.open(authUrl, '_blank')}
                              className="bg-slate-500 hover:bg-slate-600 text-white"
                            >
                              <ExternalLink className="w-4 h-4" />
                            </Button>
                          </div>
                          <Button 
                            variant="ghost"
                            size="sm"
                            onClick={regenerateAuthUrl}
                          >
                            <span className="flex items-center">
                              <RefreshCw className="w-3 h-3 mr-1" />
                              重新生成
                            </span>
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤2 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-slate-500 text-white rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      2
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        在浏览器中打开链接并完成授权
                      </p>
                      <p className="text-sm text-muted-foreground mb-2">
                        请在新标签页中打开授权链接，登录您的 OpenAI 账户并授权应用访问。
                      </p>
                      <div className="bg-muted p-3 rounded-xl border border-border">
                        <p className="text-xs text-muted-foreground flex items-start gap-2">
                          <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                          <span>
                            <strong>注意：</strong>如果您设置了代理，请确保浏览器也使用相同的代理访问授权页面。
                          </span>
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              {/* 步骤3 */}
              <Card className="bg-card/80 border-border">
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="w-6 h-6 bg-slate-500 text-white rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0">
                      3
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-foreground mb-2">
                        输入 Authorization Code
                      </p>
                      <p className="text-sm text-muted-foreground mb-3">
                        授权完成后，页面会显示一个 <strong>Authorization Code</strong>，请将其复制并粘贴到下方输入框：
                      </p>
                      <div className="space-y-3">
                        <div>
                          <Label className="flex items-center gap-2 text-sm font-semibold text-foreground mb-2">
                            <Key className="w-4 h-4 text-slate-500" />
                            Authorization Code
                          </Label>
                          <textarea 
                            value={authCode}
                            onChange={(e) => setAuthCode(e.target.value)}
                            rows={3}
                            className="w-full p-2 border border-input rounded-xl resize-none font-mono text-sm bg-background text-foreground"
                            placeholder="粘贴从OpenAI授权页面获取的Authorization Code..."
                          />
                        </div>
                        <p className="text-xs text-muted-foreground flex items-center gap-1">
                          <ExternalLink className="w-3 h-3" />
                          请粘贴从OpenAI页面复制的Authorization Code
                        </p>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  return (
    <div className="space-y-6">
      {platform === 'claude' ? renderClaudeFlow() : 
       platform === 'gemini' ? renderGeminiFlow() : 
       platform === 'openai' ? renderOpenAiFlow() :
       renderThorFlow()}
      
      <div className="flex gap-3 pt-4">
        <Button 
          type="button" 
          variant="outline"
          className="flex-1"
          onClick={onBack}
        >
          上一步
        </Button>
        <LoadingButton
          type="button" 
          disabled={!canExchange}
          loading={exchanging}
          className="flex-1"
          onClick={exchangeCode}
          loadingText="验证中..."
        >
          完成授权
        </LoadingButton>
      </div>
    </div>
  );
}