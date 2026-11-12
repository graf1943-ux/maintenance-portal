import type { Ref } from 'vue';
import { ref } from 'vue';

interface MailPreviewContext {
  notice: Ref<string>;
}

/** 案内メールのプレビュー状態と、モック送信後の通知を管理します。 */
export function useMailPreview(ctx: MailPreviewContext) {
  const showMail = ref(false);
  const mailTarget = ref('山田 太郎');
  const mailKind = ref('メンテナンスのご案内');

  function composeMail(payload: { customer: string; kind: string }) {
    mailTarget.value = payload.customer || '山田 太郎';
    mailKind.value = `${payload.kind}のご案内`;
    showMail.value = true;
  }

  function sendMail() {
    showMail.value = false;
    ctx.notice.value = `${mailTarget.value} 様への「${mailKind.value}」を送信した想定のデモです（実際には送信されません）。`;
  }

  return { showMail, mailTarget, mailKind, composeMail, sendMail };
}
